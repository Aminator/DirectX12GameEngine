#if WINDOWS_UWP
using SharpDX;
using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;

namespace DirectX12GameEngine.Graphics
{
    public sealed partial class GraphicsDevice
    {
        internal static IDirect3DDevice CreateDirect3DDevice(Device dxgiDevice)
        {
            Result result = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr graphicsDevice);

            if (result.Failure) throw new InvalidOperationException(result.Code.ToString());

            IDirect3DDevice d3DInteropDevice = (IDirect3DDevice)Marshal.GetObjectForIUnknown(graphicsDevice);
            Marshal.Release(graphicsDevice);

            return d3DInteropDevice;
        }

        internal static IDirect3DSurface CreateDirect3DSurface(Surface dxgiSurface)
        {
            Result result = CreateDirect3D11SurfaceFromDXGISurface(dxgiSurface.NativePointer, out IntPtr graphicsSurface);

            if (result.Failure) throw new InvalidOperationException(result.Code.ToString());

            IDirect3DSurface d3DSurface = (IDirect3DSurface)Marshal.GetObjectForIUnknown(graphicsSurface);
            Marshal.Release(graphicsSurface);

            return d3DSurface;
        }

        internal static Device CreateDXGIDevice(IDirect3DDevice direct3DDevice)
        {
            IDirect3DDxgiInterfaceAccess dxgiDeviceInterfaceAccess = (IDirect3DDxgiInterfaceAccess)direct3DDevice;
            IntPtr device = dxgiDeviceInterfaceAccess.GetInterface(ID3D11Resource);

            return new Device(device);
        }

        internal static Surface CreateDXGISurface(IDirect3DSurface direct3DSurface)
        {
            IDirect3DDxgiInterfaceAccess dxgiSurfaceInterfaceAccess = (IDirect3DDxgiInterfaceAccess)direct3DSurface;
            IntPtr surface = dxgiSurfaceInterfaceAccess.GetInterface(ID3D11Resource);

            return new Surface(surface);
        }
    }
}
#endif
