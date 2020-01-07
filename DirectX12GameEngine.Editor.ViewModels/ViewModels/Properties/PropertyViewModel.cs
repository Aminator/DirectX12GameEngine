using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using DirectX12GameEngine.Serialization;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Collections;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public abstract class PropertyViewModel : ViewModelBase<object>
    {
        private readonly PropertyInfo propertyInfo;

        private bool canRead;
        private bool canWrite;
        private object? index;

        public PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model)
        {
            this.propertyInfo = propertyInfo;

            CanRead = propertyInfo.CanRead && propertyInfo.GetMethod.IsPublic;
            CanWrite = propertyInfo.CanWrite && propertyInfo.SetMethod.IsPublic;

            if (model is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == propertyInfo.Name)
                    {
                        OnOwnerPropertyChanged();
                    }
                };
            }
        }

        public bool IsReadOnly => !CanWrite;

        public Type Type => Value?.GetType() ?? propertyInfo.PropertyType;

        public bool CanRead
        {
            get => canRead;
            private set => Set(ref canRead, value);
        }

        public bool CanWrite
        {
            get => canWrite;
            private set
            {
                if (Set(ref canWrite, value))
                {
                    NotifyPropertyChanged(nameof(IsReadOnly));
                }
            }
        }

        public object? Index
        {
            get => index;
            set
            {
                if (Set(ref index, value))
                {
                    NotifyPropertyChanged(nameof(PropertyName));
                }
            }
        }

        public string PropertyName
        {
            get
            {
                string propertyName = propertyInfo.Name;

                if (Index != null)
                {
                    propertyName = propertyName + " " + Index;
                }

                return propertyName;
            }
        }

        public object? Value
        {
            get => GetValue();
            set { if (CanWrite) Set(Value, value, () => SetValue(value)); }
        }

        private object? GetValue()
        {
            if (CanRead)
            {
                try
                {
                    return propertyInfo.GetValue(Model, Index is null ? null : new object[] { Index });
                }
                catch
                {
                    CanRead = false;
                }
            }

            return null;
        }

        private void SetValue(object? value)
        {
            if (CanWrite)
            {
                propertyInfo.SetValue(Model, value, Index is null ? null : new object[] { Index });
            }
        }

        protected virtual void OnOwnerPropertyChanged()
        {
            NotifyPropertyChanged(nameof(Value));
        }
    }

    public abstract class PropertyViewModel<T> : PropertyViewModel
    {
        public PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }

        public new T Value
        {
            get => (T)(base.Value ?? default(T)!);
            set => base.Value = value;
        }
    }

    public class ClassPropertyViewModel : PropertyViewModel
    {
        private bool hasUnrealizedChildren = true;
        private bool isExpanded;

        public ClassPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }

        public ObservableCollection<PropertyViewModel> Properties { get; } = new ObservableCollection<PropertyViewModel>();

        public bool HasUnrealizedChildren
        {
            get => hasUnrealizedChildren;
            set => Set(ref hasUnrealizedChildren, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (Set(ref isExpanded, value))
                {
                    if (isExpanded)
                    {
                        Fill();
                    }
                }
            }
        }

        private void Fill()
        {
            if (HasUnrealizedChildren)
            {
                object value = Value ?? throw new InvalidOperationException("Value was null.");

                var properties = ContentManager.GetDataContractProperties(Type);

                foreach (PropertyInfo property in properties)
                {
                    PropertyViewModel propertyViewModel = PropertyViewModelFactory.Default.Create(value, property);
                    Properties.Add(propertyViewModel);
                }

                HasUnrealizedChildren = false;
            }
        }
    }

    public class CollectionPropertyViewModel : PropertyViewModel
    {
        private ObservableViewModelCollection<PropertyViewModel>? items;
        private bool hasUnrealizedChildren = true;
        private bool isExpanded;

        public CollectionPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }

        public ObservableViewModelCollection<PropertyViewModel>? Items
        {
            get => items;
            set => Set(ref items, value);
        }

        public bool HasUnrealizedChildren
        {
            get => hasUnrealizedChildren;
            set => Set(ref hasUnrealizedChildren, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (Set(ref isExpanded, value))
                {
                    if (isExpanded)
                    {
                        Fill();
                    }
                }
            }
        }

        private void Fill()
        {
            if (HasUnrealizedChildren)
            {
                IList list = (IList)Value!;

                if (list.Count == 0) return;

                PropertyInfo itemProperty = typeof(IList).GetProperty("Item");

                Items = new ObservableViewModelCollection<PropertyViewModel>(list, vm => vm.Value, (m, i) => PropertyViewModelFactory.Default.Create(list, itemProperty, i));

                HasUnrealizedChildren = false;
            }
        }
    }
}
