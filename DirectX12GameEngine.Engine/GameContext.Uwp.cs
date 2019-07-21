#if WINDOWS_UWP
using System;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;
using Windows.Graphics.Holographic;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace DirectX12GameEngine.Games
{
    public class CoreWindowGameContext : GameContextWithGraphics<CoreWindow>
    {
        public CoreWindowGameContext(CoreWindow? control = null)
            : base(control ?? CoreWindow.GetForCurrentThread())
        {
            PresentationParameters.BackBufferWidth = (int)Control.Bounds.Width;
            PresentationParameters.BackBufferHeight = (int)Control.Bounds.Width;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GameWindow, CoreWindowGameWindow>();
            services.AddSingleton<GraphicsPresenter, CoreWindowSwapChainGraphicsPresenter>();
        }
    }

    public class HolographicGameContext : GameContextWithGraphics<CoreWindow>
    {
        public HolographicGameContext(HolographicSpace? holographicSpace = null, CoreWindow? control = null)
            : base(control ?? CoreWindow.GetForCurrentThread())
        {
            HolographicSpace = holographicSpace ?? HolographicSpace.CreateForCoreWindow(Control);

            PresentationParameters.BackBufferWidth = (int)Control.Bounds.Width;
            PresentationParameters.BackBufferHeight = (int)Control.Bounds.Width;
        }

        public HolographicSpace HolographicSpace { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton(HolographicSpace);
            services.AddSingleton<GameWindow, CoreWindowGameWindow>();
            services.AddSingleton<GraphicsPresenter, CoreWindowSwapChainGraphicsPresenter>();
        }
    }

    public class XamlGameContext : GameContextWithGraphics<SwapChainPanel>
    {
        public XamlGameContext(SwapChainPanel control) : base(control)
        {
            PresentationParameters.BackBufferWidth = Math.Max(1, (int)(Control.ActualWidth * Control.CompositionScaleX + 0.5f));
            PresentationParameters.BackBufferHeight = Math.Max(1, (int)(Control.ActualHeight * Control.CompositionScaleY + 0.5f));
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GameWindow, XamlGameWindow>();
            services.AddSingleton<GraphicsPresenter, XamlSwapChainGraphicsPresenter>();
        }
    }
}
#endif
