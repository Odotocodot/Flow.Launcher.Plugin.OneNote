using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Flow.Launcher.Plugin.OneNote
{
    public class OneNoteItemInfo
    {
        private Dictionary<Color, string> icons;
        private DirectoryInfo iconDirectory;
        private readonly string baseIconPath;

        public OneNoteItemInfo(string folderName, string iconName, PluginInitContext context)
        {
            this.icons = new Dictionary<Color, string>();
            this.iconDirectory = Directory.CreateDirectory(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images/" + folderName));
            this.baseIconPath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images/" + iconName);
            foreach (var fileInfo in iconDirectory.GetFiles())
            {
                if (int.TryParse(fileInfo.Name, out int argb))
                    icons.Add(Color.FromArgb(argb), fileInfo.FullName);
            }
        }
        public string GetIcon(Color color)
        {
            if (!icons.TryGetValue(color, out string path))
            {
                //Create Colored Image
                using (var bitmap = new Bitmap(baseIconPath))
                {
                    BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

                    int bytesPerPixel = Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                    byte[] pixels = new byte[bitmapData.Stride * bitmap.Height];
                    IntPtr pointer = bitmapData.Scan0;
                    Marshal.Copy(pointer, pixels, 0, pixels.Length);
                    int bytesWidth = bitmapData.Width * bytesPerPixel;

                    for (int j = 0; j < bitmapData.Height; j++)
                    {
                        int line = j * bitmapData.Stride;
                        for (int i = 0; i < bytesWidth; i = i + bytesPerPixel)
                        {
                            pixels[line + i] = color.B;
                            pixels[line + i + 1] = color.G;
                            pixels[line + i + 2] = color.R;
                        }
                    }

                    Marshal.Copy(pixels, 0, pointer, pixels.Length);
                    bitmap.UnlockBits(bitmapData);
                    path = Path.Combine(iconDirectory.FullName, color.ToArgb() + ".png");
                    bitmap.Save(path, ImageFormat.Png);
                }
                icons.Add(color, path);
            }
            return path;
        }
    }
}