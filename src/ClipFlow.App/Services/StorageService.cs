using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClipFlow.App.Models;

namespace ClipFlow.App.Services;

/// <summary>
/// Gerencia a persistência local rápida de configurações, itens do histórico,
/// itens fixados (Pins) e imagens capturadas em %LOCALAPPDATA%\ClipFlow.
/// </summary>
public sealed class StorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string BaseDir { get; }
    public string ImagesDir { get; }
    public string HistoryFilePath { get; }
    public string SettingsFilePath { get; }

    public StorageService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        BaseDir = Path.Combine(localAppData, "ClipFlow");
        ImagesDir = Path.Combine(BaseDir, "images");
        HistoryFilePath = Path.Combine(BaseDir, "history.json");
        SettingsFilePath = Path.Combine(BaseDir, "settings.json");

        Directory.CreateDirectory(BaseDir);
        Directory.CreateDirectory(ImagesDir);
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            // Usa configuração padrão em caso de arquivo corrompido
        }

        var defaults = new AppSettings();
        SaveSettings(defaults);
        return defaults;
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignora falhas de I/O transitórias
        }
    }

    public List<ClipItem> LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryFilePath))
            {
                var json = File.ReadAllText(HistoryFilePath);
                var items = JsonSerializer.Deserialize<List<ClipItem>>(json, JsonOptions);
                if (items is not null && items.Count > 0)
                {
                    return SortItems(items);
                }
            }
        }
        catch
        {
            // Se falhar, inicia histórico limpo com exemplos iniciais úteis
        }

        return CreateWelcomeItems();
    }

    public void SaveHistory(IEnumerable<ClipItem> items)
    {
        try
        {
            var list = items.ToList();
            var json = JsonSerializer.Serialize(list, JsonOptions);
            var tempFile = HistoryFilePath + ".tmp";
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, HistoryFilePath, overwrite: true);
        }
        catch
        {
            // Ignora erros transitórios de disco
        }
    }

    public string SaveImageBytes(string contentHash, byte[] pngBytes)
    {
        var fileName = $"{contentHash}.png";
        var fullPath = Path.Combine(ImagesDir, fileName);
        if (!File.Exists(fullPath))
        {
            File.WriteAllBytes(fullPath, pngBytes);
        }
        return fullPath;
    }

    public void EnforceRetentionLimit(List<ClipItem> items, int maxUnpinnedItems)
    {
        var unpinned = items.Where(x => !x.IsPinned).OrderByDescending(x => x.UpdatedAt).ToList();
        if (unpinned.Count <= maxUnpinnedItems) return;

        var toRemove = unpinned.Skip(maxUnpinnedItems).ToList();
        foreach (var item in toRemove)
        {
            items.Remove(item);
            TryDeleteOrphanImage(item, items);
        }
    }

    public void TryDeleteOrphanImage(ClipItem removedItem, IEnumerable<ClipItem> remainingItems)
    {
        if (removedItem.Kind != ClipKind.Image || string.IsNullOrWhiteSpace(removedItem.ImagePath))
            return;

        bool stillReferenced = remainingItems.Any(x =>
            string.Equals(x.ImagePath, removedItem.ImagePath, StringComparison.OrdinalIgnoreCase));

        if (!stillReferenced && File.Exists(removedItem.ImagePath))
        {
            try { File.Delete(removedItem.ImagePath); } catch { }
        }
    }

    public static List<ClipItem> SortItems(IEnumerable<ClipItem> source)
    {
        // Fixados sempre no topo, seguidos pelos mais recentes
        return source
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.UpdatedAt)
            .ToList();
    }

    private static List<ClipItem> CreateWelcomeItems()
    {
        var item1Text = "Bem-vindo ao ClipFlow! Pressione Enter ou clique em qualquer item para colá-lo automaticamente onde você estava digitando.";
        var item2Text = "#3B82F6";

        return
        [
            new ClipItem
            {
                Kind = ClipKind.Text,
                SubKind = ClipSubKind.PlainText,
                TextContent = item1Text,
                PreviewText = item1Text,
                ContentHash = WritebackGuard.ComputeTextHash(item1Text),
                TitleOrNote = "Dica Rápida (Item Fixado)",
                IsPinned = true,
                CharacterCount = item1Text.Length,
                SourceAppName = "ClipFlow",
                UpdatedAt = DateTime.UtcNow
            },
            new ClipItem
            {
                Kind = ClipKind.Text,
                SubKind = ClipSubKind.HexColor,
                TextContent = item2Text,
                PreviewText = "#3B82F6 — Azul Fluent Moderno",
                ColorHex = "#3B82F6",
                ContentHash = WritebackGuard.ComputeTextHash(item2Text),
                TitleOrNote = "Cor Primária do Tema",
                IsPinned = true,
                CharacterCount = item2Text.Length,
                SourceAppName = "ClipFlow",
                UpdatedAt = DateTime.UtcNow.AddSeconds(-10)
            }
        ];
    }
}
