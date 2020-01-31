using BepuPhysics;
using BepuPhysics.Collidables;
using Portable.Xaml.Markup;

namespace DirectX12GameEngine.Physics
{
    public class SphereColliderShape : ColliderShape
    {
        private readonly Sphere sphere;

        public SphereColliderShape(float radius)
        {
            sphere = new Sphere(radius);
        }

        [ConstructorArgument("radius")]
        public float Radius => sphere.Radius;

        public override void AddToSimulation(PhysicsSimulation simulation)
        {
            ShapeIndex = simulation.InternalSimulation.Shapes.Add(sphere);
        }

        internal override BodyInertia ComputeIntertia(float mass)
        {
            sphere.ComputeInertia(mass, out BodyInertia inertia);
            return inertia;
        }
    }
}
