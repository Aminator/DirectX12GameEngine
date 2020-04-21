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

            control.PointerCaptureLost += OnControlPointerCaptureLost;
            control.PointerEntered += OnControlPointerEntered;
            control.PointerExited += OnControlPointerExited;
            control.PointerMoved += OnControlPointerMoved;
            control.PointerPressed += OnControlPointerPressed;
            control.PointerReleased += OnControlPointerReleased;
            control.PointerWheelChanged += OnControlPointerWheelChanged;
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
            control.PointerCaptureLost -= OnControlPointerCaptureLost;
            control.PointerEntered -= OnControlPointerEntered;
            control.PointerExited -= OnControlPointerExited;
            control.PointerMoved -= OnControlPointerMoved;
            control.PointerPressed -= OnControlPointerPressed;
            control.PointerReleased -= OnControlPointerReleased;
            control.PointerWheelChanged -= OnControlPointerWheelChanged;

            base.Dispose();
        }

        private void OnControlPointerCaptureLost(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerCaptureLost(new UwpPointerEventArgs(e));
        }

        private void OnControlPointerEntered(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerEntered(new UwpPointerEventArgs(e));
        }

        private void OnControlPointerExited(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerExited(new UwpPointerEventArgs(e));
        }

        private void OnControlPointerMoved(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerMoved(new UwpPointerEventArgs(e));
        }

        private void OnControlPointerPressed(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerPressed(new UwpPointerEventArgs(e));
        }

        private void OnControlPointerReleased(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerReleased(new UwpPointerEventArgs(e));
        }

        private void OnControlPointerWheelChanged(CoreWindow sender, Windows.UI.Core.PointerEventArgs e)
        {
            OnPointerWheelChanged(new UwpPointerEventArgs(e));
        }
    }
}
