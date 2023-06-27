using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNoteItemIcons
    {
        private readonly Dictionary<Color, string> icons;
        private readonly string iconDirectory;
        private readonly string baseIconPath;
        private readonly Settings settings;

        public OneNoteItemIcons(PluginInitContext context, string folderName, string baseIcon, Settings settings)
        {
            this.settings = settings;
            icons = new Dictionary<Color, string>();
            iconDirectory = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, folderName);
            baseIconPath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, baseIcon);
            Directory.CreateDirectory(iconDirectory);
            foreach (var imagePath in Directory.EnumerateFiles(iconDirectory))
            {
                if (int.TryParse(Path.GetFileNameWithoutExtension(imagePath), out int argb))
                    icons.Add(Color.FromArgb(argb), imagePath);
            }
        }
        public int CachedIconCount => icons.Count;

        public void ClearCachedIcons()
        {
            foreach (var img in new DirectoryInfo(iconDirectory).EnumerateFiles())
            {
                img.Delete();
            }
            icons.Clear();
        }

        public long GetIconsFileSize()
        {
            return new DirectoryInfo(iconDirectory).EnumerateFiles()
                                                   .Select(file => file.Length)
                                                   .Aggregate(0L, (a, b) => a + b);
        }

        public string GetIcon(Color color)
        {
            if (icons.TryGetValue(color, out string path))
            {
                return path;
            }
            else if(!settings.CreateColoredIcons)
            {
                return baseIconPath;
            }
            else
            {
                //Create Colored Image
                using var bitmap = new Bitmap(baseIconPath);
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                byte[] pixels = new byte[bitmapData.Stride * bitmap.Height];
                IntPtr pointer = bitmapData.Scan0;
                Marshal.Copy(pointer, pixels, 0, pixels.Length);
                int bytesWidth = bitmapData.Width * bytesPerPixel;

                for (int j = 0; j < bitmapData.Height; j++)
                {
                    int line = j * bitmapData.Stride;
                    for (int i = 0; i < bytesWidth; i += bytesPerPixel)
                    {
                        pixels[line + i] = color.B;
                        pixels[line + i + 1] = color.G;
                        pixels[line + i + 2] = color.R;
                    }
                }

                Marshal.Copy(pixels, 0, pointer, pixels.Length);
                bitmap.UnlockBits(bitmapData);
                path = Path.Combine(iconDirectory, color.ToArgb() + ".png");
                    bitmap.Save(path, ImageFormat.Png);
                
                icons.Add(color, path);
                return path;
            }
        }
    }
}