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

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object parameter)
            {
                return viewModel.Settings.COMReleaseTimeout != 10
                    || viewModel.Settings.TimeType != TimeType.seconds;
            }

            public void Execute(object parameter)
            {
                viewModel.Settings.TimeType = TimeType.seconds;
                viewModel.Settings.COMReleaseTimeout = 10;
            }
        }
    }
}
