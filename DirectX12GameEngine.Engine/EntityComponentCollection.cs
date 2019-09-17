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
    }
}
