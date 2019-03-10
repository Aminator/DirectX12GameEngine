using System;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Engine
{
    public abstract class EntityComponent : IIdentifiable
    {
        public Entity? Entity { get; internal set; }

        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
