using System.Numerics;

namespace DirectX12GameEngine.Input
{
    internal class UwpPointerPoint : PointerPoint
    {
        private readonly Windows.UI.Input.PointerPoint pointerPoint;

        public UwpPointerPoint(Windows.UI.Input.PointerPoint pointerPoint)
        {
            this.pointerPoint = pointerPoint;
        }

        public override bool IsInContact => pointerPoint.IsInContact;

        public override uint PointerId => pointerPoint.PointerId;

        public override Vector2 Position => new Vector2((float)pointerPoint.Position.X, (float)pointerPoint.Position.Y);

        public override PointerPointProperties Properties => new UwpPointerPointProperties(pointerPoint.Properties);
    }
}
