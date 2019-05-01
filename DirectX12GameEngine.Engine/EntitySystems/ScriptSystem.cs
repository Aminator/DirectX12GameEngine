using System;
using System.Collections.Generic;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Engine
{
    public class ScriptSystem : EntitySystem<ScriptComponent>
    {
        private readonly HashSet<ScriptComponent> scriptsToStart = new HashSet<ScriptComponent>();

        public ScriptSystem(IServiceProvider services) : base(services)
        {
            Order = -100000;
        }

        public override void Update(GameTime gameTime)
        {
        }

        protected override void OnEntityComponentAdded(ScriptComponent component)
        {
            Add(component);
        }

        protected override void OnEntityComponentRemoved(ScriptComponent component)
        {
            Remove(component);
        }

        private void Add(ScriptComponent script)
        {
            script.Initialize(Services);

            scriptsToStart.Add(script);
        }

        private void Remove(ScriptComponent script)
        {
            bool startWasPending = scriptsToStart.Remove(script);

            if (!startWasPending)
            {
                script.Cancel();
            }
        }
    }
}
