using DirectX12GameEngine.Core;
using SharpDX.DXGI;

namespace DirectX12GameEngine.Graphics
{
    public class PresentationParameters
    {
        public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle deviceWindowHandle, PixelFormat backBufferFomat = PixelFormat.B8G8R8A8_UNorm, PixelFormat depthStencilFormat = PixelFormat.D32_Float, bool stereo = false, int syncInterval = 1, PresentParameters presentParameters = default)
        {
            BackBufferWidth = backBufferWidth;
            BackBufferHeight = backBufferHeight;
            BackBufferFormat = backBufferFomat;
            DepthStencilFormat = depthStencilFormat;
            DeviceWindowHandle = deviceWindowHandle;
            PresentParameters = presentParameters;
            Stereo = stereo;
            SyncInterval = syncInterval;
        }

        public int BackBufferWidth { get; set; }

        public int BackBufferHeight { get; set; }

        public PixelFormat BackBufferFormat { get; set; }

        public PixelFormat DepthStencilFormat { get; set; }

        public WindowHandle DeviceWindowHandle { get; set; }

        public PresentParameters PresentParameters { get; set; }

        public bool Stereo { get; set; }

        public int SyncInterval { get; set; }
    }
}
