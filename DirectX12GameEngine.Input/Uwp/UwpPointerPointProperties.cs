using System.Drawing;
using System.Numerics;

namespace DirectX12GameEngine.Input
{
    internal class UwpPointerPointProperties : PointerPointProperties
    {
        private readonly Windows.UI.Input.PointerPointProperties properties;

        public UwpPointerPointProperties(Windows.UI.Input.PointerPointProperties properties)
        {
            this.properties = properties;
        }

        public override RectangleF ContactRect => new RectangleF((float)properties.ContactRect.X, (float)properties.ContactRect.Y, (float)properties.ContactRect.Width, (float)properties.ContactRect.Height);

        public override RectangleF ContactRectRaw => new RectangleF((float)properties.ContactRect.X, (float)properties.ContactRect.Y, (float)properties.ContactRect.Width, (float)properties.ContactRect.Height);

        public override bool IsBarrelButtonPressed => properties.IsBarrelButtonPressed;

        public override bool IsCanceled => properties.IsCanceled;

        public override bool IsEraser => properties.IsEraser;

        public override bool IsHorizontalMouseWheel => properties.IsHorizontalMouseWheel;

        public override bool IsInRange => properties.IsInRange;

        public override bool IsInverted => properties.IsInverted;

        public override bool IsLeftButtonPressed => properties.IsLeftButtonPressed;

        public override bool IsMiddleButtonPressed => properties.IsMiddleButtonPressed;

        public override bool IsPrimary => properties.IsPrimary;

        public override bool IsRightButtonPressed => properties.IsRightButtonPressed;

        public override bool IsXButton1Pressed => properties.IsXButton1Pressed;

        public override bool IsXButton2Pressed => properties.IsXButton2Pressed;

        public override int MouseWheelDelta => properties.MouseWheelDelta;

        public override float Orientation => properties.Orientation;

        public override PointerUpdateKind PointerUpdateKind => (PointerUpdateKind)properties.PointerUpdateKind;

        public override float Pressure => properties.Pressure;

        public override Vector2 Tilt => new Vector2(properties.XTilt, properties.YTilt);

        public override bool TouchConfidence => properties.TouchConfidence;

        public override float Twist => properties.Twist;

        public override float? ZDistance => properties.ZDistance;

        public override int GetUsageValue(uint usagePage, uint usageId) => properties.GetUsageValue(usagePage, usageId);

        public override bool HasUsage(uint usagePage, uint usageId) => properties.HasUsage(usagePage, usageId);
    }
}
