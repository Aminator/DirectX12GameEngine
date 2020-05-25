using System;

namespace DirectX12GameEngine.Graphics
{
    public abstract class GraphicsPresenter : IDisposable
    {
        protected GraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
        {
            GraphicsDevice = device;
            PresentationParameters = presentationParameters.Clone();
            PresentationParameters.BackBufferFormat = PresentationParameters.BackBufferFormat.ToSrgb();

            DepthStencilBuffer = CreateDepthStencilBuffer();
        }

        public abstract RenderTargetView BackBuffer { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public abstract object NativePresenter { get; }

        public PresentationParameters PresentationParameters { get; }

        public DepthStencilView DepthStencilBuffer { get; protected set; }

        public virtual void BeginDraw(CommandList commandList)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DepthStencilBuffer.Resource.Dispose();
            }
        }

        public abstract void Present();

        public void Resize(int width, int height)
        {
            PresentationParameters.BackBufferWidth = width;
            PresentationParameters.BackBufferHeight = height;

            ResizeBackBuffer(width, height);
            ResizeDepthStencilBuffer(width, height);
        }

        protected virtual DepthStencilView CreateDepthStencilBuffer()
        {
            Texture depthStencilTexture = Texture.Create2D(GraphicsDevice,
                PresentationParameters.BackBufferWidth, PresentationParameters.BackBufferHeight, PresentationParameters.DepthStencilFormat,
                ResourceFlags.AllowDepthStencil | ResourceFlags.DenyShaderResource, 1, PresentationParameters.Stereo ? (short)2 : (short)1);

            return new DepthStencilView(depthStencilTexture);
        }

        protected abstract void ResizeBackBuffer(int width, int height);

        protected abstract void ResizeDepthStencilBuffer(int width, int height);
    }
}
