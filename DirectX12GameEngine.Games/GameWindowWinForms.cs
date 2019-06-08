#if NETCOREAPP
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Games
{
    internal class GameWindowWinForms : GameWindow
    {
        private readonly Control control;
        private readonly WindowHandle windowHandle;

        public GameWindowWinForms(GameBase game) : base(game)
        {
            GameContextWinForms gameContext = (GameContextWinForms)game.Context;
            control = gameContext.Control;

            windowHandle = new WindowHandle(AppContextType.WinForms, control, control.Handle);

            control.ClientSizeChanged += Control_ClientSizeChanged;
        }

        public override Rectangle ClientBounds
        {
            get
            {
                Rectangle clientRectangle = control.ClientRectangle;

                clientRectangle.Width = Math.Max(clientRectangle.Width, 1);
                clientRectangle.Height = Math.Max(clientRectangle.Height, 1);

                return clientRectangle;
            }
        }

        public override WindowHandle NativeWindow => windowHandle;

        public override void Dispose()
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        internal override void Run()
        {
            CompositionTarget.Rendering += (s, e) => Tick();
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            Tick();
        }

        private void Control_ClientSizeChanged(object sender, EventArgs e)
        {
            OnSizeChanged(e);
        }
    }
}
#endif
