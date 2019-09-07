using System.Numerics;

namespace DirectX12GameEngine.Core
{
    public static class ColorExtensions
    {
        public static System.Drawing.Color ToColor(this in Windows.UI.Color value)
        {
            return System.Drawing.Color.FromArgb(value.A, value.R, value.G, value.B);
        }

        public static Windows.UI.Color ToColor(this in System.Drawing.Color value)
        {
            return Windows.UI.Color.FromArgb(value.A, value.R, value.G, value.B);
        }
    }

    public static class PointExtensions
    {
        public static Vector2 ToVector2(this in Windows.Foundation.Point value)
        {
            return new Vector2((float)value.X, (float)value.Y);
        }

        public static Windows.Foundation.Point ToPoint(this in Vector2 value)
        {
            return new Windows.Foundation.Point(value.X, value.Y);
        }

        public static System.Drawing.PointF ToPointF(this in Windows.Foundation.Point value)
        {
            return new System.Drawing.PointF((float)value.X, (float)value.Y);
        }

        public static Windows.Foundation.Point ToPoint(this in System.Drawing.PointF value)
        {
            return new Windows.Foundation.Point(value.X, value.Y);
        }
    }

    public static class RectangleExtensions
    {
        public static System.Drawing.RectangleF ToRectangleF(this in Windows.Foundation.Rect value)
        {
            return new System.Drawing.RectangleF((float)value.X, (float)value.Y, (float)value.Width, (float)value.Height);
        }

        public static Windows.Foundation.Rect ToRect(this in System.Drawing.RectangleF value)
        {
            return new Windows.Foundation.Rect(value.X, value.Y, value.Width, value.Height);
        }
    }

    public static class SizeExtensions
    {
        public static Vector2 ToVector2(this in Windows.Foundation.Size value)
        {
            return new Vector2((float)value.Width, (float)value.Height);
        }

        public static Windows.Foundation.Size ToSize(this in Vector2 value)
        {
            return new Windows.Foundation.Size(value.X, value.Y);
        }

        public static System.Drawing.SizeF ToSizeF(this in Windows.Foundation.Size value)
        {
            return new System.Drawing.SizeF((float)value.Width, (float)value.Height);
        }

        public static Windows.Foundation.Size ToSize(this in System.Drawing.SizeF value)
        {
            return new Windows.Foundation.Size(value.Width, value.Height);
        }
    }
}
