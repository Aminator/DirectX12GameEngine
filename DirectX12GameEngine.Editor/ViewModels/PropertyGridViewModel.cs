using System;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Reflection;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;
using DirectX12GameEngine.Editor.ViewModels.Properties;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class PropertyGridViewModel : ViewModelBase
    {
        private Type? rootObjectType;
        private ObservableCollection<PropertyViewModel>? properties;

        public PropertyGridViewModel()
        {
            PropertyViewModelFactory factory = new PropertyViewModelFactory
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
                { typeof(Enum), (m, p) => new EnumPropertyViewModel(m, p) },
                { typeof(Guid), (m, p) => new GuidPropertyViewModel(m, p) },
                { typeof(DateTimeOffset?), (m, p) => new DateTimePropertyViewModel(m, p) },

                { typeof(Vector3), (m, p) => new Vector3PropertyViewModel(m, p) },
                { typeof(Vector4), (m, p) => new Vector4PropertyViewModel(m, p) },
                { typeof(Quaternion), (m, p) => new QuaternionPropertyViewModel(m, p) }
            };

            PropertyViewModelFactory.Default = factory;

            Messenger.Default.Register<ShowEntityPropertiesMessage>(this, m => ShowProperties(m.Entity));
        }

        public Type? RootObjectType
        {
            get => rootObjectType;
            set => Set(ref rootObjectType, value);
        }

        public ObservableCollection<PropertyViewModel>? Properties
        {
            get => properties;
            set => Set(ref properties, value);
        }

        private void ShowProperties(EntityViewModel entity)
        {
            RootObjectType = entity.Model.GetType();

            Properties = new ObservableCollection<PropertyViewModel>();

            var properties = ContentManager.GetDataContractProperties(RootObjectType, entity.Model);

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.Name == "Children") continue;

                PropertyViewModel propertyViewModel = PropertyViewModelFactory.Default.Create(entity.Model, propertyInfo);
                Properties.Add(propertyViewModel);
            }
        }
    }
}
