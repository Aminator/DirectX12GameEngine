using DirectX12GameEngine.PhysX;

namespace DirectX12GameEngine.Physics
{
    public static class Physics
    {
        public unsafe static void Run()
        {
            PxFoundation foundation = PhysX.PhysX.PxCreateFoundation(PhysX.PhysX.CurrentVersionAsInteger, new PxDefaultAllocator(), new PxDefaultErrorCallback());

            PxPhysics physics = PhysX.PhysX.PxCreatePhysics(PhysX.PhysX.CurrentVersionAsInteger, foundation, new PxTolerancesScale { Length = 1.0f, Speed = 9.81f });
        }
    }
}
