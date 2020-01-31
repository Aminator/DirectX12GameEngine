using BepuPhysics;
using BepuPhysics.Collidables;
using Portable.Xaml.Markup;

namespace DirectX12GameEngine.Physics
{
    public class CapsuleColliderShape : ColliderShape
    {
        private readonly Capsule capsule;

        public CapsuleColliderShape(float radius, float length)
        {
            capsule = new Capsule(radius, length);
        }

        [ConstructorArgument("radius")]
        public float Radius => capsule.Radius;

        [ConstructorArgument("length")]
        public float Length => capsule.Length;

        public override void AddToSimulation(PhysicsSimulation simulation)
        {
            ShapeIndex = simulation.InternalSimulation.Shapes.Add(capsule);
        }

        internal override BodyInertia ComputeIntertia(float mass)
        {
            capsule.ComputeInertia(mass, out BodyInertia inertia);
            return inertia;
        }
    }
}
