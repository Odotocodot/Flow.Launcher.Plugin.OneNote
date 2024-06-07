using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flow.Launcher.Plugin.OneNote.UI;
using Color = System.Drawing.Color;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Icons : BaseModel, IDisposable
    {
        public const string Logo = "Images/logo.png";
        public static string Sync => GetIconLocal("sync");
        public static string Search => GetIconLocal("search");
        public static string Recent => GetIconLocal("page_recent");
        public static string NotebookExplorer => GetIconLocal("notebook_explorer");
        public static string QuickNote => NewPage;
        public static string NewPage => GetIconLocal("page_new");
        public static string NewSection => GetIconLocal("section_new");
        public static string NewSectionGroup => GetIconLocal("section_group_new");
        public static string NewNotebook => GetIconLocal("notebook_new");
        public static string Warning => Instance.settings.IconTheme == IconTheme.Color
                ? $"Images/warning.{GetPluginThemeString(IconTheme.Light)}.png"
                : GetIconLocal("warning");
        
        private Settings settings;
        // May need this? https://stackoverflow.com/questions/21867842/concurrentdictionarys-getoradd-is-not-atomic-any-alternatives-besides-locking
        private ConcurrentDictionary<string,ImageSource> iconCache = new();
        private string imagesDirectory;

        public static DirectoryInfo GeneratedImagesDirectoryInfo { get; private set; }
        public int CachedIconCount => iconCache.Keys.Count(k => char.IsDigit(k.Split('.')[1][1]));
        public string CachedIconsFileSize => GetCachedIconsMemorySize();
        
        private PluginInitContext context;

        private static readonly Lazy<Icons> lazy = new();
        private WindowsThemeWatcher windowsThemeWatcher;
        public static Icons Instance => lazy.Value;
        
        public static void Init(PluginInitContext context, Settings settings)
        {
            Instance.imagesDirectory = $"{context.CurrentPluginMetadata.PluginDirectory}/Images/";
            
            GeneratedImagesDirectoryInfo = Directory.CreateDirectory($"{context.CurrentPluginMetadata.PluginDirectory}/Images/Generated/");

            Instance.settings = settings;
            settings.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != nameof(Settings.IconTheme)) 
                    return;
                
                if (settings.IconTheme == IconTheme.System)
                {
                    Instance.windowsThemeWatcher.StartWatching();
                }
                else
                {
                    Instance.windowsThemeWatcher.StopWatching();
                }
            };

            foreach (var image in GeneratedImagesDirectoryInfo.EnumerateFiles())
            {
                Instance.iconCache.TryAdd(image.Name, BitmapImageFromPath(image.FullName));
            }

            Instance.context = context;

            Instance.windowsThemeWatcher = new WindowsThemeWatcher();
        }
        private static string GetIconLocal(string icon) => $"Images/{icon}.{GetPluginThemeString(Instance.settings.IconTheme)}.png";

        private static string GetPluginThemeString(IconTheme iconTheme)
        {
            if (iconTheme == IconTheme.System)
            {
                iconTheme = Instance.windowsThemeWatcher.CurrentWindowsTheme.ToIconTheme();
            }
            return Enum.GetName(iconTheme).ToLower();
        }
        private static BitmapImage BitmapImageFromPath(string path) => new BitmapImage(new Uri(path));

        public static Result.IconDelegate GetIcon(IconGeneratorInfo info)
        {
            return () =>
            {
                bool generate = (string.CompareOrdinal(info.Prefix, IconGeneratorInfo.Notebook) == 0
                                 || string.CompareOrdinal(info.Prefix, IconGeneratorInfo.Section) == 0)
                                && Instance.settings.CreateColoredIcons
                                && info.Color.HasValue;
                
                if (generate)
                {
                    var imageSource = Instance.iconCache.GetOrAdd($"{info.Prefix}.{info.Color.Value.ToArgb()}.png", ImageSourceFactory,
                        info.Color.Value);
                    Instance.OnPropertyChanged(nameof(CachedIconCount));
                    Instance.OnPropertyChanged(nameof(CachedIconsFileSize));
                    return imageSource;
                }

                return Instance.iconCache.GetOrAdd($"{info.Prefix}.{GetPluginThemeString(Instance.settings.IconTheme)}.png", key =>
                {
                    var path = Path.Combine(Instance.imagesDirectory, key);
                    return BitmapImageFromPath(path);
                });

            };
        }

        private static ImageSource ImageSourceFactory(string key, Color color)
        {
            var prefix = key.Split('.')[0];
            var path = Path.Combine(Instance.imagesDirectory, $"{prefix}.dark.png");
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
                    context.API.LogException(nameof(Icons), "Failed to delete", e);
                }
                
            }
            OnPropertyChanged(nameof(CachedIconCount));
            OnPropertyChanged(nameof(CachedIconsFileSize));
        }


        private string GetCachedIconsMemorySize()
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
            // TODO unsubscribe from settings.PropertyChanged
            windowsThemeWatcher.Dispose();
        }
    }
}