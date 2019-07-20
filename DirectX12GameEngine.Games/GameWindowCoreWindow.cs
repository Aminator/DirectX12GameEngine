#if WINDOWS_UWP
using System;
using System.Drawing;
using DirectX12GameEngine.Core;
using Windows.Graphics.Display;
using Windows.UI.Core;

namespace DirectX12GameEngine.Games
{
    public class GameWindowCoreWindow : GameWindow
    {
        private readonly CoreWindow coreWindow;
        private readonly WindowHandle windowHandle;

        public GameWindowCoreWindow(GameBase game, CoreWindow coreWindow) : base(game)
        {
            this.coreWindow = coreWindow;
            windowHandle = new WindowHandle(AppContextType.CoreWindow, coreWindow);

            coreWindow.SizeChanged += CoreWindow_SizeChanged;
        }

        public override Rectangle ClientBounds
        {
            get
            {
                double resolutionScale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

                return new Rectangle((int)coreWindow.Bounds.X, (int)coreWindow.Bounds.X,
                    Math.Max(1, (int)(coreWindow.Bounds.Width * resolutionScale)), Math.Max(1, (int)(coreWindow.Bounds.Height * resolutionScale)));
            }
        }

        public override WindowHandle NativeWindow => windowHandle;

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
            OnSizeChanged(EventArgs.Empty);
        }
    }
}
#endif
