#if NETCOREAPP
using System.Windows.Forms;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public class GameContextWinForms : GameContextWithGraphics<Control>
    {
        public GameContextWinForms(Control control)
            : base(control)
        {
            PresentationParameters.WindowHandle = new WindowHandle(AppContextType.WinForms, Control, Control.Handle);

            PresentationParameters.BackBufferWidth = Control.Width;
            PresentationParameters.BackBufferHeight = Control.Height;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GameWindow, GameWindowWinForms>();
            services.AddSingleton<GraphicsPresenter, SwapChainGraphicsPresenter>();
        }
    }
}
#endif
