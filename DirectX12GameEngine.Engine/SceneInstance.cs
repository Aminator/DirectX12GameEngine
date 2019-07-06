using System;

namespace DirectX12GameEngine.Engine
{
    public sealed class SceneInstance : EntityManager
    {
        private Entity? rootEntity;

        public SceneInstance(IServiceProvider services) : base(services)
        {
        }

        public SceneInstance(IServiceProvider services, Entity rootEntity) : this(services)
        {
            RootEntity = rootEntity;
        }

        public Entity? RootEntity
        {
            get => rootEntity;
            set
            {
                if (rootEntity == value) return;

                if (rootEntity != null)
                {
                    Remove(rootEntity);
                }

                if (value != null)
                {
                    Add(value);
                }

                rootEntity = value;
            }
        }
    }
}
