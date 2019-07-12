using System;
using System.Collections.Generic;
using System.Reflection;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Engine;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EntityComponentViewModel : ViewModelBase<EntityComponent>
    {
        public EntityComponentViewModel(EntityComponent model) : base(model)
        {
            Type componentType = Model.GetType();

            TypeName = componentType.Name;
            Properties = ContentManager.GetDataContractProperties(componentType);
        }

        public string TypeName { get; }

        public Guid Id
        {
            get => Model.Id;
            set => Set(Model.Id, value, () => Model.Id = value);
        }

        public IEnumerable<PropertyInfo> Properties { get; }
    }
}
