namespace DirectX12GameEngine.Engine
{
    public class ScriptProcessor : EntitySystem<ScriptComponent>
    {
        private readonly ScriptSystem scriptSystem;

        public ScriptProcessor(ScriptSystem scriptSystem)
        {
            this.scriptSystem = scriptSystem;
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
