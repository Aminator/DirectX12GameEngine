using System;
using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public static class PixelFormatHelper
    {
        public static bool IsSRgb(this PixelFormat format) => ((Format)format).IsSRgb();

        public static PixelFormat ToSRgb(this PixelFormat format)
        {
            return ((Format)format).IsSRgb()
                ? format
                : format switch
                {
                    PixelFormat.R8G8B8A8_UNorm => PixelFormat.R8G8B8A8_UNorm_SRgb,
                    PixelFormat.B8G8R8A8_UNorm => PixelFormat.B8G8R8A8_UNorm_SRgb,
                    PixelFormat.B8G8R8X8_UNorm => PixelFormat.B8G8R8X8_UNorm_SRgb,
                    _ => throw new NotSupportedException("This format is not supported.")
                };
        }

        public static PixelFormat ToNonSRgb(this PixelFormat format)
        {
            return !((Format)format).IsSRgb()
                ? format
                : format switch
                {
                    PixelFormat.R8G8B8A8_UNorm_SRgb => PixelFormat.R8G8B8A8_UNorm,
                    PixelFormat.B8G8R8A8_UNorm_SRgb => PixelFormat.B8G8R8A8_UNorm,
                    PixelFormat.B8G8R8X8_UNorm_SRgb => PixelFormat.B8G8R8X8_UNorm,
                    _ => throw new NotSupportedException("This format is not supported.")
                };
        }
    }
}
