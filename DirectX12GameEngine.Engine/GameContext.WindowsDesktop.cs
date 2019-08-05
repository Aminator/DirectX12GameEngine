#if NETCOREAPP
using System.Windows.Forms;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Input;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public class WinFormsGameContext : GameContextWithGraphics<Control>
    {
        public WinFormsGameContext(Control control)
            : base(control)
        {
            PresentationParameters.BackBufferWidth = Control.Width;
            PresentationParameters.BackBufferHeight = Control.Height;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GameWindow>(new WinFormsGameWindow(Control));
            services.AddSingleton<GraphicsPresenter>(new HwndSwapChainGraphicsPresenter(GraphicsDevice, PresentationParameters, new WindowHandle(Control.Handle)));
            services.AddSingleton<IInputSourceConfiguration>(new WinFormsInputSourceConfiguration(Control));
        }
    }
}
#endif
