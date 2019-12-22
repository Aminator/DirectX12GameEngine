#if NETCOREAPP
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;

namespace DirectX12GameEngine.Games
{
    public class WinFormsGameWindow : GameWindow
    {
        private readonly Control control;

        public WinFormsGameWindow(Control control)
        {
            this.control = control;

            control.ClientSizeChanged += OnControlClientSizeChanged;
        }

        public override RectangleF ClientBounds
        {
            get
            {
                RectangleF clientRectangle = control.ClientRectangle;

                clientRectangle.Width = Math.Max(clientRectangle.Width, 1);
                clientRectangle.Height = Math.Max(clientRectangle.Height, 1);

                return clientRectangle;
            }
        }

        public override void Dispose()
        {
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
        }

        internal override void Run()
        {
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            Tick();
        }

        private void OnControlClientSizeChanged(object? sender, EventArgs e)
        {
            OnSizeChanged();
        }
    }
}
#endif
