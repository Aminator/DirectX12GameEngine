using System.Numerics;

namespace DirectX12GameEngine.Physics
{
    public struct HitResult
    {
        public Vector3 Normal { get; set; }

        public float T { get; set; }

        public PhysicsComponent Collider { get; set; }

        public bool Hit { get; set; }
    }
}
