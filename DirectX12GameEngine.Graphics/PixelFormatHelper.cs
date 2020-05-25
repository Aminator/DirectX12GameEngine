using Vortice.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public static class PixelFormatHelper
    {
        public static PixelFormat ToSrgb(this PixelFormat format) => format switch
        {
            PixelFormat.R8G8B8A8UIntNormalized => PixelFormat.R8G8B8A8UIntNormalizedSrgb,
            PixelFormat.B8G8R8A8UIntNormalized => PixelFormat.B8G8R8A8UIntNormalizedSrgb,
            PixelFormat.B8G8R8X8UIntNormalized => PixelFormat.B8G8R8X8UIntNormalizedSrgb,
            _ => format
        };

        public static PixelFormat ToNonSrgb(this PixelFormat format) => format switch
        {
            PixelFormat.R8G8B8A8UIntNormalizedSrgb => PixelFormat.R8G8B8A8UIntNormalized,
            PixelFormat.B8G8R8A8UIntNormalizedSrgb => PixelFormat.B8G8R8A8UIntNormalized,
            PixelFormat.B8G8R8X8UIntNormalizedSrgb => PixelFormat.B8G8R8X8UIntNormalized,
            _ => format
        };

        public static int SizeOfInBytes(this PixelFormat format) => ((Format)format).SizeOfInBytes();
    }
}
