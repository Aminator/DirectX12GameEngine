using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace DirectX12GameEngine.Input
{
    public class XamlKeyboardInputSource : KeyboardInputSourceBase
    {
        private readonly UIElement control;

        public XamlKeyboardInputSource(UIElement element)
        {
            control = element;

            control.KeyDown += OnControlKeyDown;
            control.KeyUp += OnControlKeyUp;
        }

        public override void Dispose()
        {
            control.KeyDown -= OnControlKeyDown;
            control.KeyUp -= OnControlKeyUp;
        }

        private void OnControlKeyDown(object sender, KeyRoutedEventArgs e)
        {
            OnKeyDown(new XamlKeyEventArgs(e));
        }

        private void OnControlKeyUp(object sender, KeyRoutedEventArgs e)
        {
            OnKeyUp(new XamlKeyEventArgs(e));
        }

        private class XamlKeyEventArgs : KeyEventArgs
        {
            private readonly KeyRoutedEventArgs args;

            public XamlKeyEventArgs(KeyRoutedEventArgs args)
            {
                this.args = args;
            }

            public override bool Handled { get => args.Handled; set => args.Handled = value; }

            public override VirtualKey Key => (VirtualKey)args.Key;
        }
    }
}
