using System;
using System.Drawing;
using DirectX12GameEngine.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace DirectX12GameEngine.Games
{
    public class GameWindowXaml : GameWindow
    {
        private readonly SwapChainPanel swapChainPanel;
        private readonly WindowHandle windowHandle;

        public GameWindowXaml(GameBase game, SwapChainPanel swapChainPanel) : base(game)
        {
            this.swapChainPanel = swapChainPanel;
            windowHandle = new WindowHandle(AppContextType.Xaml, swapChainPanel);

            swapChainPanel.SizeChanged += SwapChainPanel_SizeChanged;
            swapChainPanel.CompositionScaleChanged += SwapChainPanel_CompositionScaleChanged;
        }

        public override Rectangle ClientBounds
        {
            get
            {
                return new Rectangle(0, 0,
                    Math.Max(1, (int)(swapChainPanel.ActualWidth * swapChainPanel.CompositionScaleX + 0.5f)),
                    Math.Max(1, (int)(swapChainPanel.ActualHeight * swapChainPanel.CompositionScaleY + 0.5f)));
            }
        }

        public override WindowHandle NativeWindow => windowHandle;

        public override void Dispose()
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        internal override void Run()
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            Tick();
        }

        private void SwapChainPanel_CompositionScaleChanged(SwapChainPanel sender, object args)
        {
            OnSizeChanged(EventArgs.Empty);
        }

        private void SwapChainPanel_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            OnSizeChanged(EventArgs.Empty);
        }
    }
}
