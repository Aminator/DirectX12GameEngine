#if WINDOWS_UWP
using System;
using System.Drawing;
using Windows.Graphics.Display;
using Windows.UI.Core;

namespace DirectX12GameEngine.Games
{
    public class CoreWindowGameWindow : GameWindow
    {
        private readonly CoreWindow coreWindow;

        public CoreWindowGameWindow(CoreWindow coreWindow)
        {
            this.coreWindow = coreWindow;

            coreWindow.SizeChanged += CoreWindow_SizeChanged;
        }

        public override RectangleF ClientBounds
        {
            get
            {
                double resolutionScale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

                return new RectangleF((float)coreWindow.Bounds.X, (float)coreWindow.Bounds.X,
                    Math.Max(1, (float)(coreWindow.Bounds.Width * resolutionScale)), Math.Max(1, (float)(coreWindow.Bounds.Height * resolutionScale)));
            }
        }

        internal override void Run()
        {
            while (!IsExiting)
            {
                coreWindow.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);
                Tick();
            }
        }

        private void CoreWindow_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            OnSizeChanged();
        }
    }
}
#endif
