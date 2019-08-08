using DirectX12GameEngine.Core;
using System.Numerics;
using Windows.UI.Core;

namespace DirectX12GameEngine.Input
{
    public class CoreWindowPointerInputSource : PointerInputSourceBase
    {
        private readonly CoreWindow control;

        public CoreWindowPointerInputSource(CoreWindow coreWindow)
        {
            control = coreWindow;

            control.PointerCaptureLost += Control_PointerCaptureLost;
            control.PointerEntered += Control_PointerEntered;
            control.PointerExited += Control_PointerExited;
            control.PointerMoved += Control_PointerMoved;
            control.PointerPressed += Control_PointerPressed;
            control.PointerReleased += Control_PointerReleased;
            control.PointerWheelChanged += Control_PointerWheelChanged;
        }

        public override bool HasCapture => true;

        public override bool IsInputEnabled { get => control.IsInputEnabled; set => control.IsInputEnabled = value; }

        public override Cursor PointerCursor
        {
            get => new Cursor((CursorType)control.PointerCursor.Type, control.PointerCursor.Id);
            set => control.PointerCursor = new CoreCursor((CoreCursorType)value.Type, value.Id);
        }

        public override Vector2 PointerPosition { get => control.PointerPosition.ToVector2(); set => control.PointerPosition = value.ToPoint(); }

        public override void ReleasePointerCapture() => control.ReleasePointerCapture();

        public override void SetPointerCapture() => control.SetPointerCapture();

        public override void Dispose()
        {
            base.Dispose();

            control.PointerCaptureLost -= Control_PointerCaptureLost;
            control.PointerEntered -= Control_PointerEntered;
            control.PointerExited -= Control_PointerExited;
            control.PointerMoved -= Control_PointerMoved;
            control.PointerPressed -= Control_PointerPressed;
            control.PointerReleased -= Control_PointerReleased;
            control.PointerWheelChanged -= Control_PointerWheelChanged;
        }

        private void Control_PointerCaptureLost(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerCaptureLost(new UwpPointerEventArgs(e));
        }

        private void Control_PointerEntered(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerEntered(new UwpPointerEventArgs(e));
        }

        private void Control_PointerExited(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerExited(new UwpPointerEventArgs(e));
        }

        private void Control_PointerMoved(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerMoved(new UwpPointerEventArgs(e));
        }

        private void Control_PointerPressed(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerPressed(new UwpPointerEventArgs(e));
        }

        private void Control_PointerReleased(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerReleased(new UwpPointerEventArgs(e));
        }

        private void Control_PointerWheelChanged(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerWheelChanged(new UwpPointerEventArgs(e));
        }
    }
}
