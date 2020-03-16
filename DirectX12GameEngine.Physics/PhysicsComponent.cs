using System.Numerics;
using DirectX12GameEngine.Engine;

namespace DirectX12GameEngine.Physics
{
    [DefaultEntitySystem(typeof(PhysicsSystem))]
    public abstract class PhysicsComponent : EntityComponent
    {
        private ColliderShape? colliderShape;

        public PhysicsSimulation? Simulation { get; internal set; }

        public virtual bool IsTrigger { get; set; }

        public virtual ColliderShape? ColliderShape
        {
            get => colliderShape;
            set
            {
                colliderShape = value;

                if (Simulation != null)
                {
                    if (colliderShape != null && !colliderShape.ShapeIndex.Exists)
                    {
                        colliderShape.AddToSimulation(Simulation);
                    }
                }
            }
        }

        public abstract Matrix4x4 PhysicsWorldTransform { get; set; }

        internal int Handle { get; private protected set; }

        public void UpdateTransformComponent()
        {
            Matrix4x4.Decompose(Entity!.Transform.WorldMatrix, out Vector3 scale, out _, out _);

            Entity.Transform.WorldMatrix = Matrix4x4.CreateScale(scale) * PhysicsWorldTransform;
            Entity.Transform.UpdateLocalFromWorldMatrix();
        }

        internal void Attach()
        {
            OnAttach();
        }

        internal void Detach()
        {
            OnDetach();
        }

        protected virtual void OnAttach()
        {
            Entity!.Transform.UpdateWorldMatrix();

            if (colliderShape != null && !colliderShape.ShapeIndex.Exists)
            {
                colliderShape.AddToSimulation(Simulation!);
            }
        }

        protected virtual void OnDetach()
        {
        }
    }
}
