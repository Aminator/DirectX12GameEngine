using DirectX12GameEngine.Core;
using SharpDX.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class PresentationParameters
    {
        public PresentationParameters()
        {
        }

        public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle windowHandle)
            : this(backBufferWidth, backBufferHeight, windowHandle, PixelFormat.B8G8R8A8_UNorm)
        {
        }

        public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle windowHandle, PixelFormat backBufferFomat)
        {
            BackBufferWidth = backBufferWidth;
            BackBufferHeight = backBufferHeight;
            BackBufferFormat = backBufferFomat;
            WindowHandle = windowHandle;
        }

        public int BackBufferWidth { get; set; }

        public int BackBufferHeight { get; set; }

        public PixelFormat BackBufferFormat { get; set; } = PixelFormat.B8G8R8A8_UNorm;

        public PixelFormat DepthStencilFormat { get; set; } = PixelFormat.D32_Float;

        public WindowHandle? WindowHandle { get; set; }

        public PresentParameters PresentParameters { get; set; }

        public bool Stereo { get; set; }

        public int SyncInterval { get; set; } = 1;
    }
}
