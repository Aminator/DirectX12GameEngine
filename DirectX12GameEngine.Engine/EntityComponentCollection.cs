using System.Collections.ObjectModel;

namespace DirectX12GameEngine.Engine
{
    public class EntityComponentCollection : ObservableCollection<EntityComponent>
    {
        public EntityComponentCollection(Entity entity)
        {
            Entity = entity;
        }

        public Entity Entity { get; }

        protected override void InsertItem(int index, EntityComponent item)
        {
            TransformComponent? oldTransformComponent = Entity.Transform;

            base.InsertItem(index, item);

            if (item is TransformComponent)
            {
                if (oldTransformComponent != null)
                {
                    Remove(oldTransformComponent);
                }
            }
        }
    }
}
