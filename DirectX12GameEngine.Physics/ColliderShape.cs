using BepuPhysics;
using BepuPhysics.Collidables;

namespace DirectX12GameEngine.Physics
{
    public abstract class ColliderShape
    {
        internal TypedIndex ShapeIndex { get; private protected set; }

        public abstract void AddToSimulation(PhysicsSimulation simulation);

        public void RemoveFromSimulation(PhysicsSimulation simulation)
        {
            simulation.InternalSimulation.Shapes.Remove(ShapeIndex);
            ShapeIndex = default;
        }

        internal abstract BodyInertia ComputeIntertia(float mass);
    }
}
