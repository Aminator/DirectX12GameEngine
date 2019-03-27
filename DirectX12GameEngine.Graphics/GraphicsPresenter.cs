using System;
using SharpDX.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public abstract class GraphicsPresenter : IDisposable
    {
        protected GraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
        {
            GraphicsDevice = device;
            PresentationParameters = presentationParameters;

            DepthStencilBuffer = CreateDepthStencilBuffer(PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight);
        }

        public abstract Texture BackBuffer { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public abstract object NativePresenter { get; }

        public PresentationParameters PresentationParameters { get; }

        public Texture DepthStencilBuffer { get; protected set; }

        public virtual void BeginDraw(CommandList commandList)
        {
        }

        public virtual void Dispose()
        {
            DepthStencilBuffer.Dispose();
        }

        public abstract void Present();

        public void Resize(int width, int height)
        {
            PresentationParameters.BackBufferWidth = width;
            PresentationParameters.BackBufferHeight = height;

            ResizeBackBuffer(width, height);
            ResizeDepthStencilBuffer(width, height);
        }

        protected virtual Texture CreateDepthStencilBuffer(int width, int height)
        {
            return Texture.New2D(GraphicsDevice, PresentationParameters.DepthStencilFormat, width, height,
                DescriptorHeapType.DepthStencilView, ResourceStates.DepthWrite, ResourceFlags.AllowDepthStencil,
                HeapType.Default, PresentationParameters.Stereo ? (short)2 : (short)1, 1);
        }

        protected abstract void ResizeBackBuffer(int width, int height);

        protected abstract void ResizeDepthStencilBuffer(int width, int height);
    }
}
