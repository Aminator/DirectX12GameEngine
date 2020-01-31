using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public class PropertyViewModelFactory : IPropertyViewModelFactory, IEnumerable<KeyValuePair<Type, IPropertyViewModelFactory>>
    {
        private static PropertyViewModelFactory? defaultInstance;

        private readonly Dictionary<Type, IPropertyViewModelFactory> factories = new Dictionary<Type, IPropertyViewModelFactory>();

        public static PropertyViewModelFactory Default
        {
            get => defaultInstance ?? (defaultInstance = CreateDefault());
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
            object? value = null;
            
            try
            {
                value = propertyInfo.GetValue(model, index is null ? null : new object[] { index });
            }
            catch
            {
            }

            Type type = value?.GetType() ?? propertyInfo.PropertyType;

            PropertyViewModel propertyViewModel;

            if (factories.TryGetValue(type, out IPropertyViewModelFactory factory))
            {
                propertyViewModel = factory.Create(model, propertyInfo);
            }
            else
            {
                if (factories.TryGetValue(type, out IPropertyViewModelFactory? propertyFactory))
                {
                    propertyViewModel = propertyFactory.Create(model, propertyInfo);
                }
                else
                {
                    propertyViewModel = value switch
                    {
                        null => new NullPropertyViewModel(model, propertyInfo),
                        Enum _ => new EnumPropertyViewModel(model, propertyInfo),
                        IList _ => new CollectionPropertyViewModel(model, propertyInfo),
                        _ => new ClassPropertyViewModel(model, propertyInfo)
                    };
                }
            }

            propertyViewModel.Index = index;

            return propertyViewModel;
        }

        public IEnumerator<KeyValuePair<Type, IPropertyViewModelFactory>> GetEnumerator() => factories.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static PropertyViewModelFactory CreateDefault()
        {
            return new PropertyViewModelFactory
            {
                { typeof(char), (m, p) => new CharPropertyViewModel(m, p) },
                { typeof(string), (m, p) => new StringPropertyViewModel(m, p) },
                { typeof(bool), (m, p) => new BooleanPropertyViewModel(m, p) },
                { typeof(float), (m, p) => new SinglePropertyViewModel(m, p) },
                { typeof(double), (m, p) => new DoublePropertyViewModel(m, p) },
                { typeof(decimal), (m, p) => new DecimalPropertyViewModel(m, p) },
                { typeof(byte), (m, p) => new BytePropertyViewModel(m, p) },
                { typeof(sbyte), (m, p) => new SBytePropertyViewModel(m, p) },
                { typeof(short), (m, p) => new Int16PropertyViewModel(m, p) },
                { typeof(ushort), (m, p) => new UInt16PropertyViewModel(m, p) },
                { typeof(int), (m, p) => new Int32PropertyViewModel(m, p) },
                { typeof(uint), (m, p) => new UInt32PropertyViewModel(m, p) },
                { typeof(long), (m, p) => new Int64PropertyViewModel(m, p) },
                { typeof(ulong), (m, p) => new UInt64PropertyViewModel(m, p) },
                { typeof(Guid), (m, p) => new GuidPropertyViewModel(m, p) },
                { typeof(TimeSpan), (m, p) => new TimeSpanPropertyViewModel(m, p) },
                { typeof(DateTime), (m, p) => new DateTimePropertyViewModel(m, p) },
                { typeof(DateTimeOffset), (m, p) => new DateTimeOffsetPropertyViewModel(m, p) },

                { typeof(Vector2), (m, p) => new Vector2PropertyViewModel(m, p) },
                { typeof(Vector3), (m, p) => new Vector3PropertyViewModel(m, p) },
                { typeof(Vector4), (m, p) => new Vector4PropertyViewModel(m, p) },
                { typeof(Quaternion), (m, p) => new QuaternionPropertyViewModel(m, p) }
            };
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
