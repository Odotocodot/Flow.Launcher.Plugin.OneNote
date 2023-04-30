using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.OneNote.ViewModels
{
    public class SettingsViewModel
    {
        public SettingsViewModel(Settings settings)
        {
            Settings = settings;
            ResetTimeoutCommand = new ResetCommand(this);
        }
        public Settings Settings { get; init; }

        public IEnumerable<int> DefaultRecentCountOptions => Enumerable.Range(1, 16);

        public ICommand ResetTimeoutCommand { get; init; }

        private class ResetCommand : ICommand
        {
            private readonly SettingsViewModel viewModel;

            public ResetCommand(SettingsViewModel viewModel)
            {
                this.viewModel = viewModel;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                viewModel.Settings.COMReleaseTimeout = 10000;
                viewModel.Settings.TimeType = TimeType.milliseconds;
            }
        }
    }
}
