using System;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Mvvm;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EntityComponentViewModel : ViewModelBase<EntityComponent>
    {
        public EntityComponentViewModel(EntityComponent model) : base(model)
        {
            Type componentType = Model.GetType();

            TypeName = componentType.Name;
        }

        public string TypeName { get; }

        public Guid Id
        {
            get => Model.Id;
            set => Set(Model.Id, value, () => Model.Id = value);
        }
    }
}
