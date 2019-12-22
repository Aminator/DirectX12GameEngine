using Windows.UI.Core;

namespace DirectX12GameEngine.Input
{
    public class CoreWindowKeyboardInputSource : KeyboardInputSourceBase
    {
        private readonly CoreWindow control;

        public CoreWindowKeyboardInputSource(CoreWindow coreWindow)
        {
            control = coreWindow;

            control.KeyDown += OnControlKeyDown;
            control.KeyUp += OnControlKeyUp;
        }

        public override void Dispose()
        {
            control.KeyDown -= OnControlKeyDown;
            control.KeyUp -= OnControlKeyUp;
        }

        private void OnControlKeyDown(CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
            OnKeyDown(new CoreWindowKeyEventArgs(e));
        }

        private void OnControlKeyUp(CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
            OnKeyUp(new CoreWindowKeyEventArgs(e));
        }

        private class CoreWindowKeyEventArgs : KeyEventArgs
        {
            private readonly Windows.UI.Core.KeyEventArgs args;

            public CoreWindowKeyEventArgs(Windows.UI.Core.KeyEventArgs args)
            {
                this.args = args;
            }

            public override bool Handled { get => args.Handled; set => args.Handled = value; }

            public override VirtualKey Key => (VirtualKey)args.VirtualKey;
        }
    }
}
