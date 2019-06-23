using System;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public class ScriptProcessor : EntitySystem<ScriptComponent>
    {
        private readonly ScriptSystem scriptSystem;

        public ScriptProcessor(IServiceProvider services) : base(services)
        {
            scriptSystem = services.GetRequiredService<ScriptSystem>();
        }

        protected override void OnEntityComponentAdded(ScriptComponent component)
        {
            scriptSystem.Add(component);
        }

        protected override void OnEntityComponentRemoved(ScriptComponent component)
        {
            scriptSystem.Remove(component);
        }
    }
}
