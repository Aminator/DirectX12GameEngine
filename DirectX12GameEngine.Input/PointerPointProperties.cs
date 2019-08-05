using System.Drawing;
using System.Numerics;

namespace DirectX12GameEngine.Input
{
    public abstract class PointerPointProperties
    {
        public abstract RectangleF ContactRect { get; }

        public abstract RectangleF ContactRectRaw { get; }

        public abstract bool IsBarrelButtonPressed { get; }

        public abstract bool IsCanceled { get; }

        public abstract bool IsEraser { get; }

        public abstract bool IsHorizontalMouseWheel { get; }

        public abstract bool IsInRange { get; }

        public abstract bool IsInverted { get; }

        public abstract bool IsLeftButtonPressed { get; }

        public abstract bool IsMiddleButtonPressed { get; }

        public abstract bool IsPrimary { get; }

        public abstract bool IsRightButtonPressed { get; }

        public abstract bool IsXButton1Pressed { get; }

        public abstract bool IsXButton2Pressed { get; }

        public abstract int MouseWheelDelta { get; }

        public abstract float Orientation { get; }

        public abstract PointerUpdateKind PointerUpdateKind { get; }

        public abstract float Pressure { get; }

        public abstract Vector2 Tilt { get; }

        public abstract bool TouchConfidence { get; }

        public abstract float Twist { get; }

        public abstract float? ZDistance { get; }

        public abstract bool HasUsage(uint usagePage, uint usageId);

        public abstract int GetUsageValue(uint usagePage, uint usageId);
    }
}
