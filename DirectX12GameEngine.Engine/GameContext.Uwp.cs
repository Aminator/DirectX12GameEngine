using System;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Graphics.Holographic;
using DirectX12GameEngine.Input;
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

            services.AddSingleton<GameWindow>(new CoreWindowGameWindow(Control));
            services.AddSingleton<GraphicsPresenter>(new CoreWindowSwapChainGraphicsPresenter(GraphicsDevice, PresentationParameters, Control));
            services.AddSingleton<IInputSourceConfiguration>(new CoreWindowInputSourceConfiguration(Control));
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

            PresentationParameters.Stereo = HolographicDisplay.GetDefault().IsStereo;
        }

        public HolographicSpace HolographicSpace { get; }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<GameWindow>(new CoreWindowGameWindow(Control));
            services.AddSingleton<GraphicsPresenter>(new HolographicGraphicsPresenter(GraphicsDevice, PresentationParameters, HolographicSpace));
            services.AddSingleton<IInputSourceConfiguration>(new CoreWindowInputSourceConfiguration(Control));
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

            services.AddSingleton<GameWindow>(new XamlGameWindow(Control));
            services.AddSingleton<GraphicsPresenter>(new XamlSwapChainGraphicsPresenter(GraphicsDevice, PresentationParameters, Control));
            services.AddSingleton<IInputSourceConfiguration>(new XamlInputSourceConfiguration(Control));
        }
    }
}
