using System;
using DirectX12GameEngine.Engine;

#nullable enable

namespace DirectX12GameEngine.Editor
{
    public class EntityViewModel : ViewModelBase<Entity>
    {
        public EntityViewModel(Entity model) : base(model)
        {
            Children = new ObservableViewModelCollection<EntityViewModel, Entity>(Model.Children, vm => vm, m => new EntityViewModel(m));
            Components = new ObservableViewModelCollection<EntityComponentViewModel, EntityComponent>(Model.Components, vm => vm, m => new EntityComponentViewModel(m));
        }

        public string Name
        {
            get => Model.Name;
            set => Set(Model.Name, value, () => Model.Name = value);
        }

        public Guid Id
        {
            get => Model.Id;
            set => Set(Model.Id, value, () => Model.Id = value);
        }

        public ObservableViewModelCollection<EntityViewModel, Entity> Children { get; }

        public ObservableViewModelCollection<EntityComponentViewModel, EntityComponent> Components { get; }
    }
}
