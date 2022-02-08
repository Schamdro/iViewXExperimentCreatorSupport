using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;

namespace iViewXExperimentCreator.Core.Util
{

    /// <summary>
    /// Konvertiert .pdf in andere Dateitypen. (Geplant nur .png)
    /// </summary>
    public static class PDFConverter
    {
        /// <summary>
        /// Konvertiert eine .pdf-Datei in .png-Dateien. Jede Seite ergibt eine eigene Datei.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="stimDir"></param>
        public static void ConvertToPng(string filePath, string stimDir)
        {
            int dpi = 400;
            string fileName = Path.GetFileName(filePath);

            // Ghostscript benötigt eine andere DLL je nachdem, wieviel Bit das OS hat
            string version = (Environment.Is64BitProcess ? "gsdll64.dll" : "gsdll32.dll");
            GhostscriptVersionInfo versionInfo = new(version);

            // Der Rasterizer konvertiert dann die Datei
            using GhostscriptRasterizer rasterizer = new();
            // Lade die Datei als Byte-Buffer, da der Rasterizer das erwartet
            byte[] buffer = File.ReadAllBytes(filePath);
            MemoryStream ms = new(buffer);
            rasterizer.Open(ms, versionInfo, true);

            // Existierende Dateien werden beim Speichern mit (1) appendiert. Wenn (1) schon existiert, dann (2) usw.
            for (int i = 1; i <= rasterizer.PageCount; i++)
            {
                string pageName = fileName[..fileName.LastIndexOf(".")] + i;
                Image image = rasterizer.GetPage(dpi, i);
                string pageFilePath = Path.Combine(stimDir, $"{pageName}.png");
                image.Save(pageFilePath.GetFreeFilePath(), ImageFormat.Png);
            }
        }
    }
}
