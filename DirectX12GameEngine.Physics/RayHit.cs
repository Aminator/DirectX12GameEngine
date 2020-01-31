using System.Numerics;

namespace DirectX12GameEngine.Physics
{
    public struct RayHit
    {
        public Vector3 Normal { get; set; }

        public float T { get; set; }

        public bool Succeeded { get; set; }

        public PhysicsComponent Collider { get; set; }
    }
}
