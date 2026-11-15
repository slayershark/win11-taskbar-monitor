# WinTaskbarMonitor — Professional Asset & Multi-Resolution Icon Generator
Add-Type -AssemblyName System.Drawing

$generatorCode = @"
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;

public class IconGenerator {
    public static void GenerateAll(string pngDir, string icoPath, string resourcesDir) {
        int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256, 512 };
        List<byte[]> pngBytesList = new List<byte[]>();
        List<int> icoSizes = new List<int>();

        foreach (int size in sizes) {
            using (Bitmap bmp = RenderIcon(size)) {
                string pngFile = Path.Combine(pngDir, "icon-" + size + ".png");
                bmp.Save(pngFile, ImageFormat.Png);
                Console.WriteLine("Generated: " + pngFile + " (" + size + "x" + size + ")");

                // Include sizes up to 256 in the .ico
                if (size <= 256) {
                    using (MemoryStream ms = new MemoryStream()) {
                        bmp.Save(ms, ImageFormat.Png);
                        pngBytesList.Add(ms.ToArray());
                        icoSizes.Add(size);
                    }
                }
            }
        }

        // Build Multi-Resolution .ICO
        byte[] icoData = BuildIco(pngBytesList, icoSizes);
        File.WriteAllBytes(icoPath, icoData);
        Console.WriteLine("Generated Multi-Resolution ICO: " + icoPath + " (" + icoData.Length + " bytes, " + icoSizes.Count + " frames)");

        // Copy to Resources
        string resIco = Path.Combine(resourcesDir, "app.ico");
        File.Copy(icoPath, resIco, true);
        string resPng32 = Path.Combine(resourcesDir, "app_32.png");
        File.Copy(Path.Combine(pngDir, "icon-32.png"), resPng32, true);
        string resPng256 = Path.Combine(resourcesDir, "app_256.png");
        File.Copy(Path.Combine(pngDir, "icon-256.png"), resPng256, true);
        Console.WriteLine("Copied icons to Resources: " + resIco);
    }

    private static Bitmap RenderIcon(int size) {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp)) {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);

            float scale = size / 512.0f;

            // Squircle Container
            float pad = 32.0f * scale;
            float boxSize = 448.0f * scale;
            float radius = 96.0f * scale;

            using (GraphicsPath path = CreateRoundedRect(pad, pad, boxSize, boxSize, radius)) {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(24, 26, 32))) {
                    g.FillPath(bgBrush, path);
                }
                float strokeW = Math.Max(1.0f, 2.0f * scale);
                using (Pen borderPen = new Pen(Color.FromArgb(46, 51, 61), strokeW)) {
                    g.DrawPath(borderPen, path);
                }
            }

            // Pillar 1: CPU (#38BDF8)
            DrawPill(g, 130 * scale, 148 * scale, 36 * scale, 196 * scale, Color.FromArgb(56, 189, 248));

            // Pillar 2: GPU (#0EA5E9)
            DrawPill(g, 202 * scale, 228 * scale, 36 * scale, 116 * scale, Color.FromArgb(14, 165, 233));

            // Pillar 3: Peak Stream (#60A5FA)
            DrawPill(g, 274 * scale, 124 * scale, 36 * scale, 220 * scale, Color.FromArgb(96, 165, 250));

            // Pillar 4: RAM (#38BDF8)
            DrawPill(g, 346 * scale, 180 * scale, 36 * scale, 164 * scale, Color.FromArgb(56, 189, 248));

            // Taskbar Base Horizon (#0284C7)
            DrawPill(g, 106 * scale, 376 * scale, 300 * scale, 28 * scale, Color.FromArgb(2, 132, 199));
        }
        return bmp;
    }

    private static void DrawPill(Graphics g, float x, float y, float w, float h, Color color) {
        float r = Math.Min(w, h) / 2.0f;
        if (r <= 0.5f) {
            using (SolidBrush b = new SolidBrush(color)) {
                g.FillRectangle(b, x, y, Math.Max(1f, w), Math.Max(1f, h));
            }
            return;
        }
        using (GraphicsPath p = CreateRoundedRect(x, y, w, h, r)) {
            using (SolidBrush b = new SolidBrush(color)) {
                g.FillPath(b, p);
            }
        }
    }

    private static GraphicsPath CreateRoundedRect(float x, float y, float width, float height, float radius) {
        GraphicsPath path = new GraphicsPath();
        float d = radius * 2.0f;
        if (d > width) d = width;
        if (d > height) d = height;

        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static byte[] BuildIco(List<byte[]> pngs, List<int> sizes) {
        using (MemoryStream ms = new MemoryStream()) {
            BinaryWriter bw = new BinaryWriter(ms);

            // ICONDIR
            bw.Write((ushort)0); // Reserved
            bw.Write((ushort)1); // Type 1 = ICO
            bw.Write((ushort)pngs.Count); // Image count

            int offset = 6 + (pngs.Count * 16);

            for (int i = 0; i < pngs.Count; i++) {
                int s = sizes[i];
                byte w = s >= 256 ? (byte)0 : (byte)s;
                byte h = s >= 256 ? (byte)0 : (byte)s;

                bw.Write(w);              // Width
                bw.Write(h);              // Height
                bw.Write((byte)0);        // Color count
                bw.Write((byte)0);        // Reserved
                bw.Write((ushort)1);      // Color planes
                bw.Write((ushort)32);     // Bits per pixel
                bw.Write((uint)pngs[i].Length); // Size of image data
                bw.Write((uint)offset);   // Offset of image data

                offset += pngs[i].Length;
            }

            // Write PNG frame data
            for (int i = 0; i < pngs.Count; i++) {
                bw.Write(pngs[i]);
            }

            return ms.ToArray();
        }
    }
}
"@

Add-Type -TypeDefinition $generatorCode -ReferencedAssemblies System.Drawing

$pngDir = "d:\WinTaskbarMonitor\assets\png"
$icoPath = "d:\WinTaskbarMonitor\assets\app.ico"
$resDir = "d:\WinTaskbarMonitor\src\WinTaskbarMonitor\Resources"

[IconGenerator]::GenerateAll($pngDir, $icoPath, $resDir)
