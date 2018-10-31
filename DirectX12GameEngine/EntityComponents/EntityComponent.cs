using System;

namespace DirectX12GameEngine
{
    public abstract class EntityComponent
    {
        public Guid Id { get; } = Guid.NewGuid();

        public Entity? Entity { get; internal set; }
    }
}
