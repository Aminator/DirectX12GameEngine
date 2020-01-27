using DirectX12GameEngine.Engine;

namespace DirectX12GameEngine.Physics
{
    public static class PhysicsScriptComponentExtensions
    {
        public static PhysicsSimulation? GetSimulation(this ScriptComponent component)
        {
            return component.SceneSystem.SceneInstance.Systems.Get<PhysicsSystem>()?.Simulation;
        }
    }
}
