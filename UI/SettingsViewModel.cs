using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public class SettingsViewModel : BaseModel
    {
        private string notebookIcon;
        private string sectionIcon;
        private IconLooper notebookIconLooper;
        private IconLooper sectionIconLooper;
        public SettingsViewModel(PluginInitContext context, Settings settings)
        {
            Settings = settings;
            NotebookIcon = Directory.EnumerateFiles(Icons.NotebookIconDirectory).FirstOrDefault(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Icons.Notebook)); 
            SectionIcon = Directory.EnumerateFiles(Icons.SectionIconDirectory).FirstOrDefault(Path.Combine(context.CurrentPluginMetadata.PluginDirectory, Icons.Section)); 
        }

        public Settings Settings { get; init; }

#pragma warning disable CA1822 // Mark members as static
        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);
        public int CachedIconCount => Icons.CachedIconCount;
        public string CachedIconsSize => GetBytesReadable(Icons.GetIconsFileSize());
        public bool EnableClearIconButton => Icons.CachedIconCount > 0;
#pragma warning restore CA1822 // Mark members as static

        public string NotebookIcon
        {
            get => notebookIcon;
            set => SetProperty(ref notebookIcon, value);
        }
        public string SectionIcon
        {
            get => sectionIcon;
            set => SetProperty(ref sectionIcon, value);
        }
        public static void OpenNotebookIconsFolder() => Process.Start(new ProcessStartInfo { FileName = $"\"{Icons.NotebookIconDirectory}\"", UseShellExecute = true });
        public static void OpenSectionIconsFolder() => Process.Start(new ProcessStartInfo { FileName = $"\"{Icons.SectionIconDirectory}\"", UseShellExecute = true });
        public void ClearCachedIcons()
        {
            Icons.ClearCachedIcons();
            NotifyGetOnlyProperties();
        }

        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        private static string GetBytesReadable(long i)
        {
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

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, newValue))
                return false;

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }
        public void NotifyGetOnlyProperties()
        {
            OnPropertyChanged(nameof(CachedIconsSize));
            OnPropertyChanged(nameof(CachedIconCount));
            OnPropertyChanged(nameof(EnableClearIconButton));
        }
        public void OpenedFlyout()
        {
            notebookIconLooper ??= new IconLooper(Icons.NotebookIconDirectory, newValue => SetProperty(ref notebookIcon, newValue, nameof(NotebookIcon)));
            sectionIconLooper ??= new IconLooper(Icons.SectionIconDirectory, newValue => SetProperty(ref sectionIcon, newValue, nameof(SectionIcon)));
            notebookIconLooper.Start();
            sectionIconLooper.Start();
        }
        public void ClosedFlyout()
        {
            notebookIconLooper.Stop();
            sectionIconLooper.Stop();
        }

        private class IconLooper
        {
            private CancellationTokenSource tokenSource;
            private SemaphoreSlim semaphore;
            private PeriodicTimer timer;
            private IEnumerator<string> enumerator;
            private readonly string iconDirectory;
            private readonly Action<string> propertySetter;

            public IconLooper(string iconDirectory, Action<string> propertySetter)
            {
                this.iconDirectory = iconDirectory;
                this.propertySetter = propertySetter;
            }

            public void Start()
            {
                tokenSource = new CancellationTokenSource();
                semaphore ??= new SemaphoreSlim(1, 1);

                Task.Run(async () =>
                {
                    await semaphore.WaitAsync(tokenSource.Token);
                    do
                    {
                        timer = new PeriodicTimer(TimeSpan.FromSeconds(0.8));
                        enumerator = Directory.EnumerateFiles(iconDirectory).GetEnumerator();
                        if (!enumerator.MoveNext())
                            break;

                        propertySetter(enumerator.Current);

                        while (await timer.WaitForNextTickAsync(tokenSource.Token))
                        {
                            if (!enumerator.MoveNext())
                                break;

                            propertySetter(enumerator.Current);
                        }
                        timer.Dispose();
                        enumerator.Dispose();

                    } while (true);

                });
            }
            public void Stop()
            {
                tokenSource?.Cancel();
                tokenSource?.Dispose();
                semaphore?.Release();
                timer?.Dispose();
                enumerator?.Dispose();
            }
        }

    }
}
