#if WINDOWS_UWP
using System;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Graphics.Holographic;
using Microsoft.Extensions.DependencyInjection;
using Windows.Graphics.Holographic;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace DirectX12GameEngine.Games
{
    public class GameContextCoreWindow : GameContextWithGraphics<CoreWindow>
    {
        public GameContextCoreWindow(CoreWindow? control = null)
            : base(control ?? CoreWindow.GetForCurrentThread())
        {
            PresentationParameters.WindowHandle = new WindowHandle(AppContextType.CoreWindow, Control);

            PresentationParameters.BackBufferWidth = (int)Control.Bounds.Width;
            PresentationParameters.BackBufferHeight = (int)Control.Bounds.Width;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GameWindow, GameWindowCoreWindow>();
            services.AddSingleton<GraphicsPresenter, SwapChainGraphicsPresenter>();
        }
    }

    public class GameContextHolographic : GameContextWithGraphics<CoreWindow>
    {
        public GameContextHolographic(HolographicSpace? holographicSpace = null, CoreWindow? control = null)
            : base(control ?? CoreWindow.GetForCurrentThread())
        {
            HolographicSpace = holographicSpace ?? HolographicSpace.CreateForCoreWindow(Control);

            PresentationParameters.WindowHandle = new WindowHandle(AppContextType.CoreWindow, Control);

            PresentationParameters.BackBufferWidth = (int)Control.Bounds.Width;
            PresentationParameters.BackBufferHeight = (int)Control.Bounds.Width;
        }

        public HolographicSpace HolographicSpace { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton(HolographicSpace);
            services.AddSingleton<GameWindow, GameWindowCoreWindow>();
            services.AddSingleton<GraphicsPresenter, HolographicGraphicsPresenter>();
        }
    }

    public class GameContextXaml : GameContextWithGraphics<SwapChainPanel>
    {
        public GameContextXaml(SwapChainPanel control) : base(control)
        {
            PresentationParameters.WindowHandle = new WindowHandle(AppContextType.Xaml, Control);

            PresentationParameters.BackBufferWidth = Math.Max(1, (int)(Control.ActualWidth * Control.CompositionScaleX + 0.5f));
            PresentationParameters.BackBufferHeight = Math.Max(1, (int)(Control.ActualHeight * Control.CompositionScaleY + 0.5f));
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GameWindow, GameWindowXaml>();
            services.AddSingleton<GraphicsPresenter, SwapChainGraphicsPresenter>();
        }
    }
}
#endif
