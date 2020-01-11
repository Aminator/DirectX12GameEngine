using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Mvvm
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> raiser)
        {
            var propertyName = ((MemberExpression)raiser.Body).Member.Name;
            OnPropertyChanged(propertyName);
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        protected bool Set<T>(T currentValue, T newValue, Action setAction, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                setAction.Invoke();
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }
    }

    public abstract class ViewModelBase<TModel> : ViewModelBase where TModel : class
    {
        public ViewModelBase(TModel model)
        {
            Model = model;
        }

        public TModel Model { get; }
    }
}
