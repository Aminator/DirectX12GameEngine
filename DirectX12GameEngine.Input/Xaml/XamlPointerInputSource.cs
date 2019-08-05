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

        public XamlPointerInputSource(UIElement uiElement)
        {
            control = uiElement;

            control.PointerCaptureLost += Control_PointerCaptureLost;
            control.PointerEntered += Control_PointerEntered;
            control.PointerExited += Control_PointerExited;
            control.PointerMoved += Control_PointerMoved;
            control.PointerPressed += Control_PointerPressed;
            control.PointerReleased += Control_PointerReleased;
            control.PointerWheelChanged += Control_PointerWheelChanged;
        }

        public override bool HasCapture => true;

        public override bool IsInputEnabled { get => control.IsHitTestVisible; set => control.IsHitTestVisible = value; }

        public override Cursor PointerCursor
        {
            get => new Cursor((CursorType)Window.Current.CoreWindow.PointerCursor.Type, Window.Current.CoreWindow.PointerCursor.Id);
            set => Window.Current.CoreWindow.PointerCursor = new CoreCursor((CoreCursorType)value.Type, value.Id);
        }

        public override Vector2 PointerPosition { get; }

        public override void ReleasePointerCapture()
        {
            throw new System.NotImplementedException();
        }

        public override void SetPointerCapture()
        {
            throw new System.NotImplementedException();
        }

        private void Control_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            OnPointerCaptureLost(new XamlPointerEventArgs(e, control));
        }

        private void Control_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            OnPointerEntered(new XamlPointerEventArgs(e, control));
        }

        private void Control_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            OnPointerExited(new XamlPointerEventArgs(e, control));
        }

        private void Control_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            OnPointerMoved(new XamlPointerEventArgs(e, control));
        }

        private void Control_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            OnPointerPressed(new XamlPointerEventArgs(e, control));
        }

        private void Control_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            OnPointerReleased(new XamlPointerEventArgs(e, control));
        }

        private void Control_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            OnPointerWheelChanged(new XamlPointerEventArgs(e, control));
        }

        private class XamlPointerEventArgs : PointerEventArgs
        {
            private readonly PointerRoutedEventArgs args;
            private readonly UIElement control;

            public XamlPointerEventArgs(PointerRoutedEventArgs args, UIElement uiElement)
            {
                this.args = args;
                control = uiElement;
            }

            public override bool Handled { get => args.Handled; set => args.Handled = value; }

            public override PointerPoint CurrentPoint => new UwpPointerPoint(args.GetCurrentPoint(control));

            public override VirtualKeyModifiers KeyModifiers => (VirtualKeyModifiers)args.KeyModifiers;

            public override IList<PointerPoint> GetIntermediatePoints() => args.GetIntermediatePoints(control).Select(p => new UwpPointerPoint(p)).ToArray();
        }
    }
}
