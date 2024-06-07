using System;
using System.Globalization;
using System.Security.Principal;
using Microsoft.Win32;
using System.Management;

namespace Flow.Launcher.Plugin.OneNote.UI
{
	// https://stackoverflow.com/questions/59366391/is-there-any-way-to-make-a-wpf-app-respect-the-system-choice-of-dark-light-theme
	public class WindowsThemeWatcher : IDisposable
	{
		public enum WindowsTheme
		{
			Light,
			Dark
		}
		private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
		private const string RegistryValueName = "AppsUseLightTheme";
		private readonly ManagementEventWatcher watcher;
		
		public WindowsTheme CurrentWindowsTheme { get; private set; }
		public WindowsThemeWatcher()
		{
			CurrentWindowsTheme = GetWindowsTheme();
			using var currentUser = WindowsIdentity.GetCurrent();
			var query = string.Format(
				CultureInfo.InvariantCulture,
				@"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
				currentUser.User!.Value,
				RegistryKeyPath.Replace(@"\", @"\\"),
				RegistryValueName);
			
			watcher = new ManagementEventWatcher(query);
			watcher.EventArrived += OnWatcherOnEventArrived;
		}

		public void StartWatching()
		{
			try
			{
				watcher.Start();
			}
			catch (Exception)
			{
				// This can fail on Windows 7
				CurrentWindowsTheme = WindowsTheme.Light;
			}
		}
		
		public void StopWatching() => watcher.Stop();

		private void OnWatcherOnEventArrived(object sender, EventArrivedEventArgs args) =>
			CurrentWindowsTheme = GetWindowsTheme();

		private static WindowsTheme GetWindowsTheme()
		{
			using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
			var value = key?.GetValue(RegistryValueName);
			if (value is not int i)
				return WindowsTheme.Light;
			
			return i > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
		}
		
		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				watcher.EventArrived -= OnWatcherOnEventArrived;
				watcher.Stop();
				watcher.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}