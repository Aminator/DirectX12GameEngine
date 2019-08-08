using System;
using System.Drawing;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace DirectX12GameEngine.Games
{
    public class XamlGameWindow : GameWindow
    {
        private readonly SwapChainPanel swapChainPanel;

        public XamlGameWindow(SwapChainPanel swapChainPanel)
        {
            this.swapChainPanel = swapChainPanel;

            swapChainPanel.SizeChanged += SwapChainPanel_SizeChanged;
            swapChainPanel.CompositionScaleChanged += SwapChainPanel_CompositionScaleChanged;
        }

        public override RectangleF ClientBounds
        {
            get
            {
                return new RectangleF(0, 0,
                    Math.Max(1.0f, (float)swapChainPanel.ActualWidth * swapChainPanel.CompositionScaleX + 0.5f),
                    Math.Max(1, (float)swapChainPanel.ActualHeight * swapChainPanel.CompositionScaleY + 0.5f));
            }
        }

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

        private void SwapChainPanel_CompositionScaleChanged(SwapChainPanel sender, object e)
        {
            OnSizeChanged();
        }

        private void SwapChainPanel_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            OnSizeChanged();
        }
    }
}
