using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Engine
{
    public abstract class EntityComponent : IIdentifiable
    {
        public Entity? Entity { get; internal set; }

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
