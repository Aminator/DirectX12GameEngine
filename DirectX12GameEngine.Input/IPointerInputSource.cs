using System;
using System.Numerics;

namespace DirectX12GameEngine.Input
{
    public interface IPointerInputSource : IInputSource
    {
        event EventHandler<PointerEventArgs> PointerCaptureLost;

        event EventHandler<PointerEventArgs> PointerEntered;

        event EventHandler<PointerEventArgs> PointerExited;

        event EventHandler<PointerEventArgs> PointerMoved;

        event EventHandler<PointerEventArgs> PointerPressed;

        event EventHandler<PointerEventArgs> PointerReleased;

        event EventHandler<PointerEventArgs> PointerWheelChanged;

        bool HasCapture { get; }

        bool IsInputEnabled { get; set; }

        Cursor PointerCursor { get; set; }

        Vector2 PointerPosition { get; }

        void ReleasePointerCapture();

        void SetPointerCapture();
    }
}
