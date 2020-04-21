using DirectX12GameEngine.Core;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace DirectX12GameEngine.Input
{
    public class XamlPointerInputSource : PointerInputSourceBase
    {
        private readonly UIElement control;

        public XamlPointerInputSource(UIElement element)
        {
            control = element;

            control.PointerCaptureLost += OnControlPointerCaptureLost;
            control.PointerEntered += OnControlPointerEntered;
            control.PointerExited += OnControlPointerExited;
            control.PointerMoved += OnControlPointerMoved;
            control.PointerPressed += OnControlPointerPressed;
            control.PointerReleased += OnControlPointerReleased;
            control.PointerWheelChanged += OnControlPointerWheelChanged;
        }

        public override bool HasCapture => true;

        public override bool IsInputEnabled { get => control.IsHitTestVisible; set => control.IsHitTestVisible = value; }

        public override Cursor PointerCursor
        {
            get => new Cursor((CursorType)Window.Current.CoreWindow.PointerCursor.Type, Window.Current.CoreWindow.PointerCursor.Id);
            set => Window.Current.CoreWindow.PointerCursor = new CoreCursor((CoreCursorType)value.Type, value.Id);
        }

        public override Vector2 PointerPosition { get => Window.Current.CoreWindow.PointerPosition.ToVector2(); set => Window.Current.CoreWindow.PointerPosition = value.ToPoint(); }

        public override void ReleasePointerCapture() => Window.Current.CoreWindow.ReleasePointerCapture();

        public override void SetPointerCapture() => Window.Current.CoreWindow.SetPointerCapture();

        public override void Dispose()
        {
            control.PointerCaptureLost -= OnControlPointerCaptureLost;
            control.PointerEntered -= OnControlPointerEntered;
            control.PointerExited -= OnControlPointerExited;
            control.PointerMoved -= OnControlPointerMoved;
            control.PointerPressed -= OnControlPointerPressed;
            control.PointerReleased -= OnControlPointerReleased;
            control.PointerWheelChanged -= OnControlPointerWheelChanged;

            base.Dispose();
        }

        private void OnControlPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            OnPointerCaptureLost(new XamlPointerEventArgs(e, control));
        }

        private void OnControlPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            OnPointerEntered(new XamlPointerEventArgs(e, control));
        }

        private void OnControlPointerExited(object sender, PointerRoutedEventArgs e)
        {
            OnPointerExited(new XamlPointerEventArgs(e, control));
        }

        private void OnControlPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            OnPointerMoved(new XamlPointerEventArgs(e, control));
        }

        private void OnControlPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            OnPointerPressed(new XamlPointerEventArgs(e, control));
        }

        private void OnControlPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            OnPointerReleased(new XamlPointerEventArgs(e, control));
        }

        private void OnControlPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            OnPointerWheelChanged(new XamlPointerEventArgs(e, control));
        }

        private class XamlPointerEventArgs : PointerEventArgs
        {
            private readonly PointerRoutedEventArgs args;
            private readonly UIElement control;

            public XamlPointerEventArgs(PointerRoutedEventArgs args, UIElement element)
            {
                this.args = args;
                control = element;
            }

            public override bool Handled { get => args.Handled; set => args.Handled = value; }

            public override PointerPoint CurrentPoint => new UwpPointerPoint(args.GetCurrentPoint(control));

            public override VirtualKeyModifiers KeyModifiers => (VirtualKeyModifiers)args.KeyModifiers;

            public override IList<PointerPoint> GetIntermediatePoints() => args.GetIntermediatePoints(control).Select(p => new UwpPointerPoint(p)).ToArray();
        }
    }
}
