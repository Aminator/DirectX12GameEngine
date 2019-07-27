using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public class PropertyViewModelFactory : IPropertyViewModelFactory
    {
        private static PropertyViewModelFactory defaultInstance;

        private readonly Dictionary<Type, IPropertyViewModelFactory> factories = new Dictionary<Type, IPropertyViewModelFactory>();

        public static PropertyViewModelFactory Default
        {
            get => defaultInstance ?? (defaultInstance = new PropertyViewModelFactory());
            set => defaultInstance = value;
        }

        public void Add(Type type, IPropertyViewModelFactory factory)
        {
            factories.Add(type, factory);
        }

        public void Add(Type type, Func<object, PropertyInfo, PropertyViewModel> factory)
        {
            factories.Add(type, new FunctionalPropertyViewModelFactory(factory));
        }

        public PropertyViewModel Create(object model, PropertyInfo propertyInfo)
        {
            return Create(model, propertyInfo, null);
        }

        public PropertyViewModel Create(object model, PropertyInfo propertyInfo, object? index)
        {
            object? value = propertyInfo.GetValue(model, index is null ? null : new object[] { index });

            Type type = value?.GetType() ?? propertyInfo.PropertyType;

            PropertyViewModel propertyViewModel;

            if (factories.TryGetValue(type, out IPropertyViewModelFactory factory))
            {
                propertyViewModel = factory.Create(model, propertyInfo);
            }
            else
            {
                IPropertyViewModelFactory? propertyFactory = factories.FirstOrDefault(x => x.Key.IsAssignableFrom(type)).Value;

                if (propertyFactory != null)
                {
                    propertyViewModel = propertyFactory.Create(model, propertyInfo);
                }
                else
                {
                    if (value is null)
                    {
                        propertyViewModel = new NullPropertyViewModel(model, propertyInfo);
                    }
                    if (value is IList)
                    {
                        propertyViewModel = new CollectionPropertyViewModel(model, propertyInfo);
                    }
                    else
                    {
                        propertyViewModel = new ClassPropertyViewModel(model, propertyInfo);
                    }
                }
            }

            propertyViewModel.Index = index;

            return propertyViewModel;
        }
    }

    public class FunctionalPropertyViewModelFactory : IPropertyViewModelFactory
    {
        private readonly Func<object, PropertyInfo, PropertyViewModel> viewModelFactory;

        public FunctionalPropertyViewModelFactory(Func<object, PropertyInfo, PropertyViewModel> viewModelFactory)
        {
            this.viewModelFactory = viewModelFactory;
        }

        public PropertyViewModel Create(object model, PropertyInfo propertyInfo)
        {
            return viewModelFactory(model, propertyInfo);
        }
    }
}
