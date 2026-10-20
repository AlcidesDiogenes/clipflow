using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ClipFlow.App.Services;

/// <summary>
/// Renderiza o ícone vetorial moderno de alta definição (estilo Windows 11 Fluent)
/// com supersampling para a Bandeja do Sistema (System Tray), barra de título e arquivo .ico.
/// </summary>
public static class AppIconBuilder
{
    public static Bitmap RenderMasterBitmap(int size = 256)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;

        float s = size / 256f;

        // 1. Fundo Squircle com Gradiente Moderno (Azul Royal -> Violeta/Ciano)
        var bgRect = new RectangleF(16 * s, 16 * s, 224 * s, 224 * s);
        using (var bgPath = CreateRoundedRect(bgRect, 56 * s))
        using (var bgGrad = new LinearGradientBrush(
            new PointF(16 * s, 16 * s),
            new PointF(240 * s, 240 * s),
            Color.FromArgb(255, 37, 99, 235),   // #2563EB
            Color.FromArgb(255, 124, 58, 237))) // #7C3AED
        {
            g.FillPath(bgGrad, bgPath);

            // Borda sutil iluminada interna
            using var borderPen = new Pen(Color.FromArgb(95, 255, 255, 255), 4f * s);
            g.DrawPath(borderPen, bgPath);
        }

        // 2. Corpo da Prancheta (Branco translúcido/sólido nítido)
        var boardRect = new RectangleF(62 * s, 58 * s, 132 * s, 152 * s);
        using (var boardPath = CreateRoundedRect(boardRect, 22 * s))
        using (var boardBrush = new SolidBrush(Color.FromArgb(245, 248, 250, 252)))
        {
            g.FillPath(boardBrush, boardPath);
        }

        // 3. Presilha Superior da Prancheta (Âmbar / Dourado vibrante)
        var clipRect = new RectangleF(94 * s, 38 * s, 68 * s, 32 * s);
        using (var clipPath = CreateRoundedRect(clipRect, 12 * s))
        using (var clipGrad = new LinearGradientBrush(
            clipRect,
            Color.FromArgb(255, 251, 191, 36),  // #FBBF24
            Color.FromArgb(255, 217, 119, 6),   // #D97706
            LinearGradientMode.Vertical))
        {
            g.FillPath(clipGrad, clipPath);
        }

        // 4. Raio Central (ClipFlow Lightning) em Azul/Roxo profundo com brilho
        PointF[] boltPoints =
        [
            new(136 * s, 82 * s),
            new(96 * s, 138 * s),
            new(126 * s, 138 * s),
            new(114 * s, 190 * s),
            new(162 * s, 128 * s),
            new(130 * s, 128 * s)
        ];

        using (var boltPath = new GraphicsPath())
        {
            boltPath.AddPolygon(boltPoints);
            using var boltGrad = new LinearGradientBrush(
                new PointF(96 * s, 82 * s),
                new PointF(162 * s, 190 * s),
                Color.FromArgb(255, 37, 99, 235),
                Color.FromArgb(255, 79, 70, 229));
            g.FillPath(boltGrad, boltPath);
        }

        return bmp;
    }

    public static Icon CreateTrayIcon(int targetSize = 32)
    {
        using var master = RenderMasterBitmap(256);
        using var scaled = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(master, new Rectangle(0, 0, targetSize, targetSize));
        }

        return Icon.FromHandle(scaled.GetHicon());
    }

    public static BitmapSource CreateWpfWindowIcon()
    {
        using var bmp = RenderMasterBitmap(64);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.StreamSource = ms;
        bi.EndInit();
        bi.Freeze();
        return bi;
    }

    private static GraphicsPath CreateRoundedRect(RectangleF bounds, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
