using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace Flow.Launcher.Plugin.OneNote.Icons
{
    public class IconProvider : BaseModel, IDisposable
    {
        public const string Logo = "Images/logo.png";
        public string Sync => GetIconLocal("sync");
        public string Search => GetIconLocal("search");
        public string Recent => GetIconLocal("page_recent");
        public string NotebookExplorer => GetIconLocal("notebook_explorer");
        public string QuickNote => NewPage;
        public string NewPage => GetIconLocal("page_new");
        public string NewSection => GetIconLocal("section_new");
        public string NewSectionGroup => GetIconLocal("section_group_new");
        public string NewNotebook => GetIconLocal("notebook_new");
        public string Warning => settings.IconTheme == IconTheme.Color
                ? $"Images/warning.{GetPluginThemeString(IconTheme.Light)}.png"
                : GetIconLocal("warning");
        
        private readonly Settings settings;
        // May need this? https://stackoverflow.com/questions/21867842/concurrentdictionarys-getoradd-is-not-atomic-any-alternatives-besides-locking
        private readonly ConcurrentDictionary<string,ImageSource> iconCache = new();
        private readonly string imagesDirectory;

        public DirectoryInfo GeneratedImagesDirectoryInfo { get; }
        public int CachedIconCount => iconCache.Keys.Count(k => char.IsDigit(k.Split('.')[1][1]));
        
        private readonly PluginInitContext context;
        
        private readonly WindowsThemeWatcher windowsThemeWatcher;

        public IconProvider(PluginInitContext context, Settings settings)
        {
            imagesDirectory = $"{context.CurrentPluginMetadata.PluginDirectory}/Images/";
            
            GeneratedImagesDirectoryInfo = Directory.CreateDirectory($"{context.CurrentPluginMetadata.PluginDirectory}/Images/Generated/");

            this.context = context;
            this.settings = settings;
            
            windowsThemeWatcher = new WindowsThemeWatcher();

            if (settings.IconTheme == IconTheme.System)
            {
                windowsThemeWatcher.StartWatching();
            }

            settings.PropertyChanged += OnIconThemeChanged;

            foreach (var image in GeneratedImagesDirectoryInfo.EnumerateFiles())
            {
                iconCache.TryAdd(image.Name, BitmapImageFromPath(image.FullName));
            }
        }

        private void OnIconThemeChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(Settings.IconTheme)) 
                return;
            
            if (settings.IconTheme == IconTheme.System)
            {
                windowsThemeWatcher.StartWatching();
            }
            else
            {
                windowsThemeWatcher.StopWatching();
            }
        }

        private string GetIconLocal(string icon) => $"Images/{icon}.{GetPluginThemeString(settings.IconTheme)}.png";

        private string GetPluginThemeString(IconTheme iconTheme)
        {
            if (iconTheme == IconTheme.System)
            {
                iconTheme = windowsThemeWatcher.CurrentWindowsTheme.ToIconTheme();
            }
            return Enum.GetName(iconTheme).ToLower();
        }
        private static BitmapImage BitmapImageFromPath(string path) => new BitmapImage(new Uri(path));

        public Result.IconDelegate GetIcon(IconGeneratorInfo info)
        {
            return () =>
            {
                bool generate = (string.CompareOrdinal(info.Prefix, IconGeneratorInfo.Notebook) == 0
                                 || string.CompareOrdinal(info.Prefix, IconGeneratorInfo.Section) == 0)
                                && settings.CreateColoredIcons
                                && info.Color.HasValue;
                
                if (generate)
                {
                    var imageSource = iconCache.GetOrAdd($"{info.Prefix}.{info.Color.Value.ToArgb()}.png", ImageSourceFactory,
                        info.Color.Value);
                    OnPropertyChanged(nameof(CachedIconCount));
                    return imageSource;
                }

                return iconCache.GetOrAdd($"{info.Prefix}.{GetPluginThemeString(settings.IconTheme)}.png", key =>
                {
                    var path = Path.Combine(imagesDirectory, key);
                    return BitmapImageFromPath(path);
                });

            };
        }

        private ImageSource ImageSourceFactory(string key, Color color)
        {
            var prefix = key.Split('.')[0];
            var path = Path.Combine(imagesDirectory, $"{prefix}.dark.png");
            var bitmap =  BitmapImageFromPath(path);
            var newBitmap = ChangeIconColor(bitmap, color);
                                
            path = $"{GeneratedImagesDirectoryInfo.FullName}{key}";
            // https://stackoverflow.com/questions/65860129/pngbitmapencoder-failling

            using var fileStream = new FileStream(path, FileMode.Create);
            var encoder = new PngBitmapEncoder(); //TODO Lazy load this and only one
            encoder.Frames.Add(BitmapFrame.Create(newBitmap));
            encoder.Save(fileStream);
            // encoder.Frames.Clear();
            return newBitmap;
        }
        
        private static BitmapSource ChangeIconColor(BitmapImage bitmapImage, Color color)
        {
            var writeableBitmap = new WriteableBitmap(bitmapImage);
            
            var stride = writeableBitmap.BackBufferStride;
            var pixelHeight = writeableBitmap.PixelHeight;
            
            var pixelData = new byte[stride * pixelHeight];
            var bytesPerPixel = writeableBitmap.Format.BitsPerPixel / 8;
            
            writeableBitmap.CopyPixels(pixelData, stride, 0);
            for (int j = 0; j < pixelHeight; j++)
            {
                int line = j * stride;
                for (int i = 0; i < stride; i += bytesPerPixel)
                {
                    pixelData[line + i] = color.B;
                    pixelData[line + i + 1] = color.G;
                    pixelData[line + i + 2] = color.R;
                }
            }
            writeableBitmap.WritePixels(new Int32Rect(0, 0, writeableBitmap.PixelWidth, pixelHeight),
                pixelData, stride, 0);
            
            return writeableBitmap;
        }

        public void ClearCachedIcons()
        {
            iconCache.Clear();
            foreach (var file in GeneratedImagesDirectoryInfo.EnumerateFiles())
            {
                try
                {
                    file.Delete();
                }
                catch (Exception e)
                {
                    // context.API.ShowMsg("Failed to delete", $"Failed to delete {file.Name}");
                    context.API.LogException(nameof(IconProvider), "Failed to delete", e);
                }
                
            }
            OnPropertyChanged(nameof(CachedIconCount));
        }


        public string GetCachedIconsMemorySize()
        {
            var i = GeneratedImagesDirectoryInfo.EnumerateFiles()
                .Select(file => file.Length)
                .Aggregate(0L, (a, b) => a + b);
            
            // Returns the human-readable file size for an arbitrary, 64-bit file size 
            // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
            // Get absolute value
            long absolute_i = Math.Abs(i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            switch (absolute_i)
            {
                case >= 0x40000000: // Gigabyte 
                    suffix = "GB";
                    readable = i >> 20;
                    break;
                case >= 0x100000: // Megabyte 
                    suffix = "MB";
                    readable = i >> 10;
                    break;
                case >= 0x400:
                    suffix = "KB"; // Kilobyte 
                    readable = i;
                    break;
                default:
                    return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable /= 1024;
            // Return formatted number with suffix
            return readable.ToString("0.## ") + suffix;
        }

        public void Dispose()
        {
            settings.PropertyChanged -= OnIconThemeChanged;
            windowsThemeWatcher.Dispose();
        }
    }
}