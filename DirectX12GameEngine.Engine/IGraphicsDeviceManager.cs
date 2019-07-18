using DirectX12GameEngine.Graphics;

namespace DirectX12GameEngine.Engine
{
    public interface IGraphicsDeviceManager
    {
        GraphicsDevice? GraphicsDevice { get; }

        bool BeginDraw();

        void CreateDevice();

        void EndDraw();
    }
}
