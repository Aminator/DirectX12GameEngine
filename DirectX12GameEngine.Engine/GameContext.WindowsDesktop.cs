#if NETCOREAPP
using System.Windows.Forms;
using DirectX12GameEngine.Graphics;
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

            services.AddSingleton(new WindowHandle(Control.Handle));
            services.AddSingleton<GameWindow, WinFormsGameWindow>();
            services.AddSingleton<GraphicsPresenter, HwndSwapChainGraphicsPresenter>();
        }
    }
}
#endif
