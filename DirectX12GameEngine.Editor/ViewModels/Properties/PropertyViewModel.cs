using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DirectX12GameEngine.Core.Assets;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public abstract class PropertyViewModel : ViewModelBase<object>
    {
        private readonly PropertyInfo propertyInfo;

        private object? index;

        public PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model)
        {
            this.propertyInfo = propertyInfo;

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

        public Type Type => Value?.GetType() ?? propertyInfo.PropertyType;

        public object? Value
        {
            get => GetValue();
            set => Set(Value, value, () => SetValue(value));
        }

        private object? GetValue() => propertyInfo.GetValue(Model, Index is null ? null : new object[] { Index });

        private void SetValue(object? value) => propertyInfo.SetValue(Model, value, Index is null ? null : new object[] { Index });

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
            get => (T)base.Value!;
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
                object value = Value!;

                var properties = ContentManager.GetDataContractProperties(Type, value);

                foreach (PropertyInfo property in properties)
                {
                    PropertyViewModel propertyViewModel = PropertyViewModelFactory.Default.Create(value, property);
                    Properties.Add(propertyViewModel);
                }

                HasUnrealizedChildren = false;
            }
        }

        public ObservableCollection<PropertyViewModel> Properties { get; } = new ObservableCollection<PropertyViewModel>();
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
