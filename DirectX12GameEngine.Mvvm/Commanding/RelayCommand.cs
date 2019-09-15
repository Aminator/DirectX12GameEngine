using System;
using System.Windows.Input;

namespace DirectX12GameEngine.Mvvm.Commanding
{
    public class RelayCommand : ICommand
    {
        private readonly Func<bool>? canExecute;
        private readonly Action execute;

        public RelayCommand(Action execute)
        {
            this.execute = execute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute) : this(execute)
        {
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return canExecute is null ? true : canExecute();
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                execute();
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Func<T, bool>? canExecute;
        private readonly Action<T> execute;

        public RelayCommand(Action<T> execute)
        {
            this.execute = execute;
        }

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute) : this(execute)
        {
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return canExecute is null ? true : canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                execute((T)parameter);
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
