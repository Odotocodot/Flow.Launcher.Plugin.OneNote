#nullable enable
using System;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.OneNote.UI
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Predicate<object?>? canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke(parameter) != false;
        }
        
        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }
}