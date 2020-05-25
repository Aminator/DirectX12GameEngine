using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Physics
{
    public class PhysicsSystem : EntitySystem<PhysicsComponent>
    {
        public PhysicsSystem() : base(typeof(TransformComponent))
        {
        }

        public PhysicsSimulation Simulation { get; } = new PhysicsSimulation();

        public override void Update(GameTime gameTime)
        {
            foreach (var rigidBody in Simulation.RigidBodies)
            {
                rigidBody.Value.PhysicsWorldTransform = rigidBody.Value.Entity!.Transform.WorldMatrix;
            }

            Simulation.Timestep(gameTime.Elapsed);

            foreach (var rigidBody in Simulation.RigidBodies)
            {
                rigidBody.Value.UpdateTransformComponent();
            }
        }

        protected override void OnEntityComponentAdded(PhysicsComponent component)
        {
            component.Simulation = Simulation;
            component.Attach();
        }

        protected override void OnEntityComponentRemoved(PhysicsComponent component)
        {
            component.Detach();
            component.Simulation = null;
        }
    }
}
