using DirectX12GameEngine.Engine;

namespace DirectX12GameEngine.Physics
{
    public static class PhysicsScriptComponentExtensions
    {
        public static PhysicsSimulation? GetSimulation(this ScriptComponent component)
        {
            return component.SceneSystem.Systems.Get<PhysicsSystem>()?.Simulation;
        }
    }
}
