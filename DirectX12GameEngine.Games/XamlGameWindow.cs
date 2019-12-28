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

            swapChainPanel.SizeChanged += OnSwapChainPanelSizeChanged;
            swapChainPanel.CompositionScaleChanged += OnSwapChainPanelCompositionScaleChanged;
        }

        public override RectangleF ClientBounds
        {
            get
            {
                return new RectangleF(0, 0,
                    Math.Max(1.0f, (float)swapChainPanel.ActualWidth * swapChainPanel.CompositionScaleX + 0.5f),
                    Math.Max(1.0f, (float)swapChainPanel.ActualHeight * swapChainPanel.CompositionScaleY + 0.5f));
            }
        }

        public override void Dispose()
        {
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
        }

        public override void Run()
        {
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        private void OnCompositionTargetRendering(object? sender, object e)
        {
            Tick();
        }

        private void OnSwapChainPanelCompositionScaleChanged(SwapChainPanel sender, object e)
        {
            OnSizeChanged();
        }

        private void OnSwapChainPanelSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            OnSizeChanged();
        }
    }
}
