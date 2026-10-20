using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ClipFlow.App.Models;
using ClipFlow.App.Platform;

namespace ClipFlow.App.Services;

/// <summary>
/// Serviço central de monitoramento do Clipboard do Windows e registro do Atalho Global.
/// Inclui mecanismo de retry para COMException (CLIPBRD_E_CANT_OPEN) e WritebackGuard.
/// </summary>
public sealed class ClipboardService : IDisposable
{
    private const int HotkeyIdPrimary = 1001;
    private const int MaxRetryAttempts = 8;
    private const uint VK_C = 0x43;

    private readonly MessageWindow _messageWindow;
    private readonly DispatcherTimer _retryTimer;
    private readonly StorageService _storage;
    private readonly WritebackGuard _writebackGuard = new();

    private uint _lastObservedSequence;
    private int _retryAttempts;
    private bool _isReading;
    private bool _isSettingClipboard;
    private bool _disposed;

    public AppSettings Settings { get; set; }
    public bool IsPaused { get; set; }
    public bool HotkeyRegistered { get; private set; }

    public event Action<ClipItem>? ItemCaptured;
    public event Action? TogglePopupRequested;
    public event Action? ExitRequested;

    public ClipboardService(StorageService storage, AppSettings settings)
    {
        _storage = storage;
        Settings = settings;

        _messageWindow = new MessageWindow(OnWindowMessage);
        NativeMethods.AddClipboardFormatListener(_messageWindow.Handle);

        RegisterConfiguredHotkey(Settings.Shortcut);

        _retryTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(75)
        };
        _retryTimer.Tick += OnRetryTimerTick;
    }

    public bool RegisterConfiguredHotkey(ShortcutChoice choice)
    {
        if (HotkeyRegistered)
        {
            NativeMethods.UnregisterHotKey(_messageWindow.Handle, HotkeyIdPrimary);
            HotkeyRegistered = false;
        }

        uint modifiers = NativeMethods.MOD_NOREPEAT | choice switch
        {
            ShortcutChoice.AltV or ShortcutChoice.AltC => NativeMethods.MOD_ALT,
            ShortcutChoice.CtrlAltV => NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            _ => NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT
        };

        uint virtualKey = choice is ShortcutChoice.CtrlShiftC or ShortcutChoice.AltC
            ? VK_C
            : NativeMethods.VK_V;

        HotkeyRegistered = NativeMethods.RegisterHotKey(
            _messageWindow.Handle,
            HotkeyIdPrimary,
            modifiers,
            virtualKey);

        Settings.Shortcut = choice;
        return HotkeyRegistered;
    }

    private bool OnWindowMessage(int msg, nint wParam, nint lParam)
    {
        if (_disposed) return false;

        if (msg == NativeMethods.WM_CLOSE)
        {
            ExitRequested?.Invoke();
            return true;
        }

        if (msg == NativeMethods.WM_HOTKEY && wParam == HotkeyIdPrimary)
        {
            TogglePopupRequested?.Invoke();
            return true;
        }

        if (msg == NativeMethods.WM_CLIPBOARDUPDATE && !IsPaused && !_isSettingClipboard)
        {
            var seq = NativeMethods.GetClipboardSequenceNumber();
            if (seq != _writebackGuard.OwnSequenceNumber)
            {
                _lastObservedSequence = seq;
                _retryAttempts = 0;
                _retryTimer.Start();
            }
            return true;
        }

        return false;
    }

    private void OnRetryTimerTick(object? sender, EventArgs e)
    {
        if (_disposed || IsPaused || _isSettingClipboard)
        {
            _retryTimer.Stop();
            return;
        }

        if (_isReading) return;
        _isReading = true;

        try
        {
            var (allowed, sourceAppName) = PrivacyGuard.CheckClipboardSourceApp(Settings);
            if (!allowed)
            {
                _retryTimer.Stop();
                return;
            }

            var dataObject = System.Windows.Clipboard.GetDataObject();
            if (dataObject is null)
            {
                _retryTimer.Stop();
                return;
            }

            if (PrivacyGuard.ShouldIgnoreDataObject(dataObject, Settings))
            {
                _retryTimer.Stop();
                return;
            }

            var item = ExtractClipItem(dataObject, sourceAppName);
            _retryTimer.Stop();

            if (item is null) return;

            var currentSeq = NativeMethods.GetClipboardSequenceNumber();
            if (_writebackGuard.ShouldSkip(item.ContentHash, currentSeq))
            {
                return;
            }

            ItemCaptured?.Invoke(item);
        }
        catch (COMException)
        {
            if (++_retryAttempts >= MaxRetryAttempts)
            {
                _retryTimer.Stop();
            }
        }
        catch
        {
            _retryTimer.Stop();
        }
        finally
        {
            _isReading = false;
        }
    }

    private ClipItem? ExtractClipItem(System.Windows.IDataObject data, string? sourceAppName)
    {
        int maxTextBytes = Math.Clamp(Settings.MaxTextSizeKb, 64, 8192) * 1024;

        // 1. Texto Unicode / Texto Comum
        if (Settings.CaptureText &&
            (data.GetDataPresent(System.Windows.DataFormats.UnicodeText) || data.GetDataPresent(System.Windows.DataFormats.Text)))
        {
            var text = (data.GetData(System.Windows.DataFormats.UnicodeText) ?? data.GetData(System.Windows.DataFormats.Text)) as string;
            if (string.IsNullOrWhiteSpace(text) || text.Length > maxTextBytes)
                return null;

            if (PrivacyGuard.ContainsHighConfidenceSecret(text, Settings))
                return null;

            var hash = WritebackGuard.ComputeTextHash(text);
            var (subKind, hexColor) = ContentDetector.ClassifyText(text);
            var preview = ContentDetector.BuildPreviewText(text);

            return new ClipItem
            {
                Kind = ClipKind.Text,
                SubKind = subKind,
                TextContent = text,
                PreviewText = preview,
                ColorHex = hexColor,
                ContentHash = hash,
                CharacterCount = text.Length,
                SourceAppName = sourceAppName,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // 2. Imagem / Print Screen
        if (Settings.CaptureImages && data.GetDataPresent(System.Windows.DataFormats.Bitmap))
        {
            var bitmap = System.Windows.Clipboard.GetImage();
            if (bitmap is null || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
                return null;

            if ((long)bitmap.PixelWidth * bitmap.PixelHeight > 20_000_000)
                return null;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            var pngBytes = ms.ToArray();

            var hash = WritebackGuard.ComputeBytesHash(pngBytes);
            var imagePath = _storage.SaveImageBytes(hash, pngBytes);

            return new ClipItem
            {
                Kind = ClipKind.Image,
                SubKind = ClipSubKind.PlainText,
                PreviewText = $"Imagem capturada ({bitmap.PixelWidth} × {bitmap.PixelHeight} px)",
                ImagePath = imagePath,
                ContentHash = hash,
                ImageWidth = bitmap.PixelWidth,
                ImageHeight = bitmap.PixelHeight,
                SourceAppName = sourceAppName,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // 3. Lista de Arquivos Copiados no Explorer
        if (Settings.CaptureFiles && data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            if (data.GetData(System.Windows.DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                var joined = string.Join(Environment.NewLine, files);
                var hash = WritebackGuard.ComputeTextHash(joined);
                var fileNames = string.Join(", ", files.Take(3).Select(Path.GetFileName));
                if (files.Length > 3) fileNames += $" (+{files.Length - 3})";

                return new ClipItem
                {
                    Kind = ClipKind.Files,
                    SubKind = ClipSubKind.FilePath,
                    TextContent = joined,
                    PreviewText = fileNames,
                    FilePaths = files,
                    ContentHash = hash,
                    CharacterCount = files.Length,
                    SourceAppName = sourceAppName ?? "Explorer",
                    UpdatedAt = DateTime.UtcNow
                };
            }
        }

        return null;
    }

    public bool PlaceItemOnClipboard(ClipItem item, bool plainTextOnly = false)
    {
        if (_disposed) return false;

        _writebackGuard.Suppress(item.ContentHash);
        _isSettingClipboard = true;

        try
        {
            var data = new System.Windows.DataObject();

            switch (item.Kind)
            {
                case ClipKind.Text:
                    var text = item.TextContent ?? string.Empty;
                    data.SetText(text, System.Windows.TextDataFormat.UnicodeText);
                    if (!plainTextOnly && !string.IsNullOrWhiteSpace(item.HtmlContent))
                    {
                        data.SetData(System.Windows.DataFormats.Html, item.HtmlContent);
                    }
                    break;

                case ClipKind.Image:
                    if (plainTextOnly)
                    {
                        data.SetText(item.ImagePath ?? item.PreviewText);
                    }
                    else if (!string.IsNullOrWhiteSpace(item.ImagePath) && File.Exists(item.ImagePath))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(item.ImagePath, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        data.SetImage(bmp);
                    }
                    break;

                case ClipKind.Files:
                    if (plainTextOnly)
                    {
                        data.SetText(item.TextContent ?? string.Empty);
                    }
                    else if (item.FilePaths is { Length: > 0 })
                    {
                        var existing = item.FilePaths.Where(File.Exists).Concat(item.FilePaths.Where(Directory.Exists)).Distinct().ToArray();
                        if (existing.Length > 0)
                        {
                            var collection = new StringCollection();
                            collection.AddRange(existing);
                            data.SetFileDropList(collection);
                        }
                        else
                        {
                            data.SetText(item.TextContent ?? string.Empty);
                        }
                    }
                    break;
            }

            System.Windows.Clipboard.SetDataObject(data, copy: true);
            _writebackGuard.OwnSequenceNumber = NativeMethods.GetClipboardSequenceNumber();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _isSettingClipboard = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _retryTimer.Stop();
        _retryTimer.Tick -= OnRetryTimerTick;

        if (HotkeyRegistered)
        {
            NativeMethods.UnregisterHotKey(_messageWindow.Handle, HotkeyIdPrimary);
        }

        NativeMethods.RemoveClipboardFormatListener(_messageWindow.Handle);
        _messageWindow.Dispose();
    }
}
