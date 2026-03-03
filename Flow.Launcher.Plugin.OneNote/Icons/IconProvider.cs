using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using IC = Flow.Launcher.Plugin.OneNote.Icons.IconConstants;

namespace Flow.Launcher.Plugin.OneNote.Icons
{
    public class IconProvider : BaseModel
    {
        private readonly PluginInitContext context;
        private readonly Settings settings;
        private readonly string imagesDirectory;
        private readonly ConcurrentDictionary<string, ImageSource> iconCache = new();
        
        public const string Logo = IC.ImagesDirectory + IC.Logo + ".png";
        public string Sync => GetIconPath(IC.Sync);
        public string Search => GetIconPath(IC.Search);
        public string Recent => GetIconPath(IC.Recent);
        public string NotebookExplorer => GetIconPath(IC.NotebookExplorer);
        public string QuickNote => NewPage;
        public string NewPage => GetIconPath(IC.NewPage);
        public string NewSection => GetIconPath(IC.NewSection);
        public string NewSectionGroup => GetIconPath(IC.NewSectionGroup);
        public string NewNotebook => GetIconPath(IC.NewNotebook);
        public string Warning => settings.IconTheme == IconTheme.Color
            ? $"{IC.ImagesDirectory}{IC.Warning}.{GetIconThemeString(IconTheme.Dark)}.png"
            : GetIconPath(IC.Warning);
        public static GlyphInfo Clipboard { get; } = new("/Resources/#Segoe Fluent Icons", "\uf0e3"); // Clipboard

        public int CachedIconCount => iconCache.Keys.Count(k => char.IsDigit(k.Split('.')[1][1]));
        public DirectoryInfo GeneratedImagesDirectoryInfo { get; }


        public IconProvider(PluginInitContext context, Settings settings)
        {
            imagesDirectory = $"{context.CurrentPluginMetadata.PluginDirectory}/{IC.ImagesDirectory}";
            
            GeneratedImagesDirectoryInfo = Directory.CreateDirectory($"{context.CurrentPluginMetadata.PluginDirectory}/{IC.GeneratedImagesDirectory}");

            this.context = context;
            this.settings = settings;

            foreach (var image in GeneratedImagesDirectoryInfo.EnumerateFiles())
            {
                var imageSource = BitmapImageFromPath(image.FullName);
                imageSource.Freeze();
                iconCache.TryAdd(image.Name, imageSource);
            }
        }
        
        private string GetIconPath(string icon) => $"{IC.ImagesDirectory}{icon}.{GetIconThemeString(settings.IconTheme)}.png";

        private static string GetIconThemeString(IconTheme iconTheme)
        {
            if (iconTheme == IconTheme.System)
            {
                iconTheme = FlowLauncherThemeToIconTheme();
            }
            return iconTheme.ToString().ToLower();
        }
        
        private static IconTheme FlowLauncherThemeToIconTheme()
        {
            var color05B = (SolidColorBrush)Application.Current.TryFindResource("Color05B"); //Alt key "SystemControlPageTextBaseHighBrush"
            if (color05B == null)
                return IconTheme.Light;

            var color = color05B.Color;
            return 5 * color.G + 2 * color.R + color.B > 8 * 128 //Is the color light?
                ? IconTheme.Light 
                : IconTheme.Dark;
        }
        
        private static BitmapImage BitmapImageFromPath(string path) => new BitmapImage(new Uri(path));
        
        public Result.IconDelegate GetIcon(IconGeneratorInfo info)
        { 
            bool generate = (string.CompareOrdinal(info.prefix, IC.Notebook) == 0 
                             || string.CompareOrdinal(info.prefix, IC.Section) == 0)
                            && settings.CreateColoredIcons 
                            && info.color.HasValue;

            return generate ? GetIconGenerated : GetIconStatic;

            ImageSource GetIconGenerated()
            {
                var imageSource = iconCache.GetOrAdd($"{info.prefix}.{info.color!.Value.ToArgb()}.png",
                    static (key, t) => ImageSourceFactory(t.self, key, t.Color), 
                    (Color: info.color.Value, self: this));
                OnPropertyChanged(nameof(CachedIconCount));
                return imageSource;
            }
            
            ImageSource GetIconStatic()
            {
                return iconCache.GetOrAdd($"{info.prefix}.{GetIconThemeString(settings.IconTheme)}.png", 
                    static (key,dir) => BitmapImageFromPath(Path.Combine(dir, key)),
                    imagesDirectory);
            }
        }

        private static ImageSource ImageSourceFactory(IconProvider self, string key, Color color)
        {
            var prefix = key.Split('.')[0];
            var path = Path.Combine(self.imagesDirectory, $"{prefix}.dark.png");
            var bitmap =  BitmapImageFromPath(path);
            var newBitmap = ChangeIconColor(bitmap, color);
                                
            path = $"{self.GeneratedImagesDirectoryInfo.FullName}{key}";

            using var fileStream = new FileStream(path, FileMode.Create);
            var encoder = new PngBitmapEncoder(); 
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
            writeableBitmap.Freeze();
            
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
    }
}