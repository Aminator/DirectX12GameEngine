using Windows.UI.Core;

namespace DirectX12GameEngine.Input
{
    public class CoreWindowKeyboardInputSource : KeyboardInputSourceBase
    {
        private readonly CoreWindow control;

        public CoreWindowKeyboardInputSource(CoreWindow coreWindow)
        {
            control = coreWindow;

            control.KeyDown += Control_KeyDown;
            control.KeyUp += Control_KeyUp;
        }

        public override void Dispose()
        {
            control.KeyDown -= Control_KeyDown;
            control.KeyUp -= Control_KeyUp;
        }

        private void Control_KeyDown(CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            OnKeyDown(new CoreWindowKeyEventArgs(args));
        }

        private void Control_KeyUp(CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            OnKeyUp(new CoreWindowKeyEventArgs(args));
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
