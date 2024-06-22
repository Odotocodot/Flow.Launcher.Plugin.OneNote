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
        
        private readonly Settings settings;
        private readonly ConcurrentDictionary<string,ImageSource> iconCache = new();
        private readonly string imagesDirectory;

        public DirectoryInfo GeneratedImagesDirectoryInfo { get; }
        public int CachedIconCount => iconCache.Keys.Count(k => char.IsDigit(k.Split('.')[1][1]));
        
        private readonly PluginInitContext context;

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
            return Enum.GetName(iconTheme).ToLower();
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
            bool generate = (string.CompareOrdinal(info.Prefix, IC.Notebook) == 0 
                             || string.CompareOrdinal(info.Prefix, IC.Section) == 0)
                            && settings.CreateColoredIcons 
                            && info.Color.HasValue;

            return generate ? GetIconGenerated : GetIconStatic;

            ImageSource GetIconGenerated()
            {
                var imageSource = iconCache.GetOrAdd($"{info.Prefix}.{info.Color!.Value.ToArgb()}.png", ImageSourceFactory, info.Color.Value);
                OnPropertyChanged(nameof(CachedIconCount));
                return imageSource;
            }
            
            ImageSource GetIconStatic()
            {
                return iconCache.GetOrAdd($"{info.Prefix}.{GetIconThemeString(settings.IconTheme)}.png", key =>
                {
                    var path = Path.Combine(imagesDirectory, key);
                    return BitmapImageFromPath(path);
                });
            }
        }

        private ImageSource ImageSourceFactory(string key, Color color)
        {
            var prefix = key.Split('.')[0];
            var path = Path.Combine(imagesDirectory, $"{prefix}.dark.png");
            var bitmap =  BitmapImageFromPath(path);
            var newBitmap = ChangeIconColor(bitmap, color);
                                
            path = $"{GeneratedImagesDirectoryInfo.FullName}{key}";

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