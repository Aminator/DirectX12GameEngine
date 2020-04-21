using System;
using System.Windows.Input;

namespace Microsoft.Toolkit.Mvvm.Commands
{
    public class RelayCommand : ICommand
    {
        public RelayCommand() : this(null, null)
        {
        }

        public RelayCommand(Action? execute) : this(execute, null)
        {
        }

        public RelayCommand(Action? execute, Func<bool>? canExecute)
        {
            ExecuteRequested = execute;
            CanExecuteRequested = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public Func<bool>? CanExecuteRequested { get; set; }

        public Action? ExecuteRequested { get; set; }

        public bool CanExecute(object? parameter)
        {
            return CanExecuteRequested is null ? true : CanExecuteRequested.Invoke();
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                ExecuteRequested?.Invoke();
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        public RelayCommand() : this(null, null)
        {
        }

        public RelayCommand(Action<T>? execute) : this(execute, null)
        {
        }

        public RelayCommand(Action<T>? execute, Func<T, bool>? canExecute)
        {
            ExecuteRequested = execute;
            CanExecuteRequested = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public Func<T, bool>? CanExecuteRequested { get; set; }

        public Action<T>? ExecuteRequested { get; set; }

        public bool CanExecute(object parameter)
        {
            return CanExecuteRequested?.Invoke((T)parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                ExecuteRequested?.Invoke((T)parameter);
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ObjectRelayCommand : RelayCommand<object>
    {
        public ObjectRelayCommand()
        {
        }

        public ObjectRelayCommand(Action<object>? execute) : base(execute)
        {
        }

        public ObjectRelayCommand(Action<object>? execute, Func<object, bool>? canExecute) : base(execute, canExecute)
        {
        }
    }
}
