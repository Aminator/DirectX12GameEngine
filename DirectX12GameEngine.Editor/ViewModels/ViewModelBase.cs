using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyPropertyChanged<T>(Expression<Func<T>> raiser)
        {
            var propName = ((MemberExpression)raiser.Body).Member.Name;
            NotifyPropertyChanged(propName);
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                NotifyPropertyChanged(name);
                return true;
            }

            return false;
        }

        protected bool Set<T>(T currentValue, T newValue, Action setAction, [CallerMemberName] string? property = null)
        {
            if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                setAction.Invoke();
                NotifyPropertyChanged(property);
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
