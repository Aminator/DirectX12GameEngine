using System;
using System.Collections.Generic;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Engine
{
    public sealed class LightSystem : EntitySystem<LightComponent>
    {
        public LightSystem(IServiceProvider services) : base(services)
        {
        }

        public IReadOnlyList<LightComponent> Lights => Components;

        public override void Draw(GameTime gameTime)
        {
            foreach (LightComponent light in Lights)
            {
                light.Update();
            }
        }
    }
}
