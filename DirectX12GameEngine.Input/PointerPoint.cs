using System.Numerics;

namespace DirectX12GameEngine.Input
{
    public abstract class PointerPoint
    {
        public abstract bool IsInContact { get; }

        public abstract uint PointerId { get; }

        public abstract Vector2 Position { get; }

        public abstract PointerPointProperties Properties { get; }
    }
}
