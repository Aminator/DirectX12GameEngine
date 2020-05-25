#if NETCOREAPP
using System.Windows.Forms;

namespace DirectX12GameEngine.Input
{
    public class WinFormsKeyboard : KeyboardInputSourceBase
    {
        private readonly Control control;

        public WinFormsKeyboard(Control control)
        {
            this.control = control;

            control.KeyDown += OnControlKeyDown;
            control.KeyUp += OnControlKeyUp;
        }

        private void OnControlKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            OnKeyDown(new WinFormsKeyEventArgs(e));
        }

        private void OnControlKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            OnKeyUp(new WinFormsKeyEventArgs(e));
        }

        private class WinFormsKeyEventArgs : KeyEventArgs
        {
            private readonly System.Windows.Forms.KeyEventArgs args;

            public WinFormsKeyEventArgs(System.Windows.Forms.KeyEventArgs args)
            {
                this.args = args;
            }

            public override bool Handled { get => args.Handled; set => args.Handled = value; }

            public override VirtualKey Key => (VirtualKey)args.KeyCode;
        }
    }
}
#endif
