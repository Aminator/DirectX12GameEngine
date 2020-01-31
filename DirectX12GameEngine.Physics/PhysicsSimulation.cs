using System;
using System.Buffers;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Microsoft.Collections.Extensions;

namespace DirectX12GameEngine.Physics
{
    public sealed class PhysicsSimulation : IDisposable
    {
        private readonly BufferPool bufferPool;
        private readonly NarrowPhaseCallbacks narrowPhaseCallbacks;
        private readonly PoseIntegratorCallbacks poseIntegratorCallbacks;

        public PhysicsSimulation()
        {
            bufferPool = new BufferPool();
            narrowPhaseCallbacks = new NarrowPhaseCallbacks();
            poseIntegratorCallbacks = new PoseIntegratorCallbacks(new Vector3(0.0f, -9.81f, 0.0f));

            InternalSimulation = Simulation.Create(bufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks);
        }

        public DictionarySlim<int, RigidBodyComponent> RigidBodies { get; } = new DictionarySlim<int, RigidBodyComponent>();

        public DictionarySlim<int, StaticColliderComponent> StaticColliders { get; } = new DictionarySlim<int, StaticColliderComponent>();

        internal Simulation InternalSimulation { get; }

        public void Dispose()
        {
            InternalSimulation.Dispose();
            bufferPool.Clear();
        }

        public RayHit RayCast(in Vector3 origin, in Vector3 direction, float maximumT)
        {
            RayCast(origin, direction, maximumT, out RayHit hit);

            return hit;
        }

        public bool RayCast(in Vector3 origin, in Vector3 direction, float maximumT, out RayHit hit)
        {
            using var memory = MemoryPool<RayHit>.Shared.Rent(1);
            RayCast(origin, direction, maximumT, memory.Memory);

            hit = memory.Memory.Span[0];
            return hit.Succeeded;
        }

        public void RayCast(in Vector3 origin, in Vector3 direction, float maximumT, Memory<RayHit> hits)
        {
            RayHitHandler rayHitHandler = new RayHitHandler(this, hits);
            InternalSimulation.RayCast(origin, direction, maximumT, ref rayHitHandler);
        }

        public void Timestep(TimeSpan deltaTime)
        {
            InternalSimulation.Timestep((float)deltaTime.TotalSeconds);
        }

        private struct RayHitHandler : IRayHitHandler
        {
            public RayHitHandler(PhysicsSimulation simulation, Memory<RayHit> hits) : this()
            {
                Simulation = simulation;
                Hits = hits;
            }

            public PhysicsSimulation Simulation { get; }

            public Memory<RayHit> Hits { get; }

            public int IntersectionCount { get; private set; }

            public bool AllowTest(CollidableReference collidable)
            {
                return true;
            }

            public bool AllowTest(CollidableReference collidable, int childIndex)
            {
                return true;
            }

            public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
            {
                ref RayHit hit = ref Hits.Span[IntersectionCount++];

                hit.Normal = normal;
                hit.T = t;
                hit.Succeeded = true;

                hit.Collider = collidable.Mobility == CollidableMobility.Static
                    ? (PhysicsComponent)Simulation.StaticColliders.GetOrAddValueRef(collidable.Handle)
                    : Simulation.RigidBodies.GetOrAddValueRef(collidable.Handle);
            }
        }

        private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            public void Initialize(Simulation simulation)
            {
            }

            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
            {
                return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
            }

            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
            {
                return true;
            }

            public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold>
            {
                pairMaterial.FrictionCoefficient = 1.0f;
                pairMaterial.MaximumRecoveryVelocity = 2.0f;
                pairMaterial.SpringSettings = new SpringSettings(30.0f, 1.0f);

                return true;
            }

            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
            {
                return true;
            }

            public void Dispose()
            {
            }
        }

        private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            private Vector3 gravityDeltaTime;

            public PoseIntegratorCallbacks(Vector3 gravity) : this()
            {
                Gravity = gravity;
            }

            public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

            public Vector3 Gravity { get; set; }

            public void PrepareForIntegration(float dt)
            {
                gravityDeltaTime = Gravity * dt;
            }

            public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
            {
                if (localInertia.InverseMass > 0)
                {
                    velocity.Linear += gravityDeltaTime;
                }
            }
        }
    }
}
