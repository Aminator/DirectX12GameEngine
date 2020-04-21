using System;

namespace DirectX12GameEngine.Engine
{
    public sealed class SceneSystem : EntityManager
    {
        private Entity? rootEntity;

        public SceneSystem(IServiceProvider services) : base(services)
        {
        }

        public CameraComponent? CurrentCamera { get; set; }

        public Entity? RootEntity
        {
            get => rootEntity;
            set
            {
                if (rootEntity == value) return;

                if (rootEntity != null)
                {
                    RemoveRoot(rootEntity);
                }

                if (value != null)
                {
                    AddRoot(value);
                }

                rootEntity = value;
            }
        }
    }
}
