using System;
using System.Numerics;
using BepuPhysics;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Physics
{
    public class StaticColliderComponent : PhysicsComponent
    {
        internal StaticHandle Handle { get; private protected set; }

        public override ColliderShape? ColliderShape
        {
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value), "Static colliders cannot lack a shape. Their only purpose is colliding.");

                base.ColliderShape = value;

                if (Simulation != null)
                {
                    Simulation.InternalSimulation.Statics.GetDescription(Handle, out StaticDescription description);

                    description.Collidable.Shape = value.ShapeIndex;
                    Simulation.InternalSimulation.Statics.ApplyDescription(Handle, description);
                }
            }
        }

        public override Matrix4x4 PhysicsWorldTransform
        {
            get
            {
                if (Simulation is null) return Matrix4x4.Identity;

                Simulation.InternalSimulation.Statics.GetDescription(Handle, out StaticDescription description);

                return Matrix4x4.CreateFromQuaternion(description.Pose.Orientation) * Matrix4x4.CreateTranslation(description.Pose.Position);
            }
            set
            {
                if (Simulation != null)
                {
                    Simulation.InternalSimulation.Statics.GetDescription(Handle, out StaticDescription description);

                    (_, description.Pose.Orientation, description.Pose.Position) = value;

                    Simulation.InternalSimulation.Statics.ApplyDescription(Handle, description);
                }
            }
        }

        protected override void OnAttach()
        {
            base.OnAttach();

            if (ColliderShape is null) throw new ArgumentNullException(nameof(ColliderShape), "Static colliders cannot lack a shape. Their only purpose is colliding.");

            Matrix4x4.Decompose(Entity!.Transform.WorldMatrix, out _, out Quaternion rotation, out Vector3 translation);
            StaticDescription description = new StaticDescription(translation, rotation, ColliderShape.ShapeIndex, 0.1f);

            Handle = Simulation!.InternalSimulation.Statics.Add(description);
            Simulation.StaticColliders.GetOrAddValueRef(Handle.Value) = this;
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            Simulation!.InternalSimulation.Statics.Remove(Handle);
            Simulation.StaticColliders.Remove(Handle.Value);
        }
    }
}
