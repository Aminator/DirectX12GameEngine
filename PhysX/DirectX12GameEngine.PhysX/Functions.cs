using System;

namespace DirectX12GameEngine.PhysX
{
    public static partial class PhysX
    {
        public static PxPhysics PxCreatePhysics(uint version, PxFoundation foundation, PxTolerancesScale scale, bool trackOutstandingAllocations = false, IntPtr pvd = default)
        {
            PxPhysics physics = PxCreateBasePhysics(version, foundation, scale, trackOutstandingAllocations, pvd);

            PxRegisterArticulations(physics);
            PxRegisterArticulationsReducedCoordinate(physics);
            PxRegisterHeightFields(physics);

            return physics;
        }
    }
}
