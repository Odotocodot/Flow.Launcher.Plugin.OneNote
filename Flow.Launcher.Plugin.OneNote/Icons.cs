using Odotocodot.OneNote.Linq;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace Flow.Launcher.Plugin.OneNote
{
    public class Icons// : BaseModel
    {
        
        public const string Logo = "Images/logo.png";
        
        public static string Sync => GetIconLocal("sync");

        public static string Warning => pluginTheme == PluginTheme.Color
                ? $"Images/warning.{GetPluginThemeString(PluginTheme.Dark)}.png"
                : GetIconLocal("warning");

        public static string Search => GetIconLocal("search");
        //public static string RecycleBin => GetIconLocal("recycle_bin");
        public static string Recent => GetIconLocal("page_recent");
        public static string NotebookExplorer => GetIconLocal("notebook_explorer");
        public static string QuickNote => NewPage;
        // public const string Page = Logo;
        // public const string Section = "Images/section.png";
        // public const string SectionGroup = "Images/section_group.png";
        // public const string Notebook = "Images/notebook.png";
        public static string NewPage => GetIconLocal("page_new");
        public static string NewSection => GetIconLocal("section_new");
        public static string NewSectionGroup => GetIconLocal("section_group_new");
        public static string NewNotebook => GetIconLocal("notebook_new");

        // private OneNoteItemIcons notebookIcons;
        // private OneNoteItemIcons sectionIcons;
        private Settings settings;
        private static PluginTheme pluginTheme;

        private static string GetPluginThemeString(PluginTheme pluginTheme)
        {
            if (pluginTheme == PluginTheme.System)
                throw new NotImplementedException(); //TODO get the system theme return either light or dark.
            return Enum.GetName(pluginTheme).ToLower();
        }

        private static string GetIconLocal(string icon) => $"Images/{icon}.{GetPluginThemeString(pluginTheme)}.png";

        // May need this? https://stackoverflow.com/questions/21867842/concurrentdictionarys-getoradd-is-not-atomic-any-alternatives-besides-locking
        private ConcurrentDictionary<string,ImageSource> iconCache = new();
        private string imagesDirectory;

        public static DirectoryInfo GeneratedImagesDirectoryInfo { get; private set; }

        //TODO: Update on use UI
        public int CachedIconCount => iconCache.Keys.Count(k => char.IsDigit(k.Split('.')[1][1]));

        //TODO: UPdate on use for UI
        public string CachedIconsFileSize => GetCachedIconsMemorySize();//GetBytesReadable(notebookIcons.IconsFileSize + sectionIcons.IconsFileSize);
        // public static string NotebookIconDirectory { get; private set; }
        // public static string SectionIconDirectory { get; private set; }


        private static readonly Lazy<Icons> lazy = new();
        public static Icons Instance => lazy.Value;

        public static void Init(PluginInitContext context, Settings settings)
        {
            pluginTheme = settings.PluginTheme;
            // NotebookIconDirectory = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images", "NotebookIcons");
            // SectionIconDirectory = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, "Images", "SectionIcons");
            //
            // Instance.notebookIcons = new OneNoteItemIcons(NotebookIconDirectory, Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Notebook));
            // Instance.sectionIcons = new OneNoteItemIcons(SectionIconDirectory, Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Section));

            Instance.imagesDirectory = $"{context.CurrentPluginMetadata.PluginDirectory}/Images/";
            // Instance.GeneratedImagesDirectory = $"{context.CurrentPluginMetadata.PluginDirectory}/Images/Generated/";
            
            GeneratedImagesDirectoryInfo = Directory.CreateDirectory($"{context.CurrentPluginMetadata.PluginDirectory}/Images/Generated/");
            // Instance.notebookIcons.PropertyChanged += Instance.IconCountChanged;
            // Instance.sectionIcons.PropertyChanged += Instance.IconCountChanged;

            Instance.settings = settings;

            foreach (var image in GeneratedImagesDirectoryInfo.EnumerateFiles())
            {
                Instance.iconCache.TryAdd(image.Name, BitmapImageFromPath(image.FullName));
            }
        }

        public static void Close()
        {
            // Instance.notebookIcons.PropertyChanged -= Instance.IconCountChanged;
            // Instance.sectionIcons.PropertyChanged -= Instance.IconCountChanged;
        }
        // private void IconCountChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        // {
        //     OnPropertyChanged(nameof(CachedIconCount));
        //     OnPropertyChanged(nameof(CachedIconsFileSize));
        // }
        private static BitmapImage BitmapImageFromPath(string path) => new BitmapImage(new Uri(path));

        public static Result.IconDelegate GetIcon(string prefix, Color? color)
        {
            return () =>
            {
                bool generate = (string.CompareOrdinal(prefix, "notebook") == 0
                                 || string.CompareOrdinal(prefix, "section") == 0)
                                && Instance.settings.CreateColoredIcons
                                && color.HasValue;

                if (generate)
                {
                    return Instance.iconCache.GetOrAdd($"{prefix}.{color.Value.ToArgb()}.png", ImageSourceFactory,
                        color.Value);
                }

                return Instance.iconCache.GetOrAdd($"{prefix}.{GetPluginThemeString(pluginTheme)}.png", key =>
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
                file.Delete();
            }
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

    }
}