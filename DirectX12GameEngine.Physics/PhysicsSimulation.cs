using System;
using System.Buffers;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities.Memory;

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

        internal Simulation InternalSimulation { get; }

        public void Dispose()
        {
            InternalSimulation.Dispose();
            bufferPool.Clear();
        }

        public HitResult RayCast(in Vector3 origin, in Vector3 direction, float maximumT)
        {
            using var memory = MemoryPool<HitResult>.Shared.Rent(128);

            RayHitHandler rayHitHandler = new RayHitHandler(memory.Memory);
            InternalSimulation.RayCast(origin, direction, maximumT, ref rayHitHandler);

            return rayHitHandler.Hits.Span[0];
        }

        public void Timestep(TimeSpan deltaTime)
        {
            InternalSimulation.Timestep((float)deltaTime.TotalSeconds);
        }

        private struct RayHitHandler : IRayHitHandler
        {
            public RayHitHandler(Memory<HitResult> hits) : this()
            {
                Hits = hits;
            }

            public Memory<HitResult> Hits { get; set; }

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
                maximumT = t;
                ref HitResult hit = ref Hits.Span[ray.Id];

                if (t < hit.T)
                {
                    if (hit.T == float.MaxValue)
                    {
                        IntersectionCount++;
                    }

                    hit.Normal = normal;
                    hit.T = t;
                    //hit.Collidable = collidable;
                    hit.Hit = true;
                }
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
