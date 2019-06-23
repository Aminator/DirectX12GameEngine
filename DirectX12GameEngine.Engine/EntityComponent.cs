using System;
using System.Runtime.Serialization;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Engine
{
    public abstract class EntityComponent : IIdentifiable
    {
        [IgnoreDataMember]
        public Entity? Entity { get; internal set; }

        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
