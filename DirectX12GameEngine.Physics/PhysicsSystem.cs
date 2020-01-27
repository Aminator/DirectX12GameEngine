using System;
using System.Collections.Generic;
using System.Numerics;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Physics
{
    public class PhysicsSystem : EntitySystem<PhysicsComponent>
    {
        private readonly List<RigidBodyComponent> rigidBodies = new List<RigidBodyComponent>();

        public PhysicsSystem() : base(typeof(TransformComponent))
        {
            Order = -10000;
        }

        public PhysicsSimulation Simulation { get; } = new PhysicsSimulation();

        public override void Update(GameTime gameTime)
        {
            Simulation.Timestep(gameTime.Elapsed);

            UpdateRigidBodyTransforms();
        }

        private void UpdateRigidBodyTransforms()
        {
            foreach (var rigidBody in rigidBodies)
            {
                rigidBody.UpdateTransformComponent();
            }
        }

        protected override void OnEntityComponentAdded(PhysicsComponent component)
        {
            component.Simulation = Simulation;
            component.Attach();

            if (component is RigidBodyComponent rigidBody)
            {
                rigidBodies.Add(rigidBody);
            }
        }

        protected override void OnEntityComponentRemoved(PhysicsComponent component)
        {
            component.Detach();
            component.Simulation = null;

            if (component is RigidBodyComponent rigidBody)
            {
                rigidBodies.Remove(rigidBody);
            }
        }
    }

    public static class QuaternionExtensions
    {
        public static Quaternion ToQuaternion(this in BepuUtilities.Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static BepuUtilities.Quaternion ToQuaternion(this in Quaternion q)
        {
            return new BepuUtilities.Quaternion(q.X, q.Y, q.Z, q.W);
        }
    }
}
