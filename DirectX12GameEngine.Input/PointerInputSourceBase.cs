using System;
using System.Numerics;

namespace DirectX12GameEngine.Input
{
    public abstract class PointerInputSourceBase : IPointerInputSource
    {
        private Vector2 capturedPointerPosition;
        private bool isPointerPositionLocked;

        public event EventHandler<PointerEventArgs> PointerCaptureLost;

        public event EventHandler<PointerEventArgs> PointerEntered;

        public event EventHandler<PointerEventArgs> PointerExited;

        public event EventHandler<PointerEventArgs> PointerMoved;

        public event EventHandler<PointerEventArgs> PointerPressed;

        public event EventHandler<PointerEventArgs> PointerReleased;

        public event EventHandler<PointerEventArgs> PointerWheelChanged;

        public abstract bool HasCapture { get; }

        public abstract bool IsInputEnabled { get; set; }

        public bool IsPointerPositionLocked
        {
            get => isPointerPositionLocked;
            set
            {
                if (isPointerPositionLocked != value)
                {
                    isPointerPositionLocked = value;

                    if (isPointerPositionLocked)
                    {
                        capturedPointerPosition = PointerPosition;
                    }
                }
            }
        }

        public abstract Cursor PointerCursor { get; set; }

        public abstract Vector2 PointerPosition { get; set; }

        public virtual void Dispose()
        {
        }

        public abstract void ReleasePointerCapture();

        public abstract void SetPointerCapture();

        public virtual void Scan()
        {
        }

        public virtual void Update()
        {
        }

        protected virtual void OnPointerCaptureLost(PointerEventArgs e)
        {
            PointerCaptureLost?.Invoke(this, e);
        }

        protected virtual void OnPointerEntered(PointerEventArgs e)
        {
            PointerEntered?.Invoke(this, e);
        }

        protected virtual void OnPointerExited(PointerEventArgs e)
        {
            PointerExited?.Invoke(this, e);
        }

        protected virtual void OnPointerMoved(PointerEventArgs e)
        {
            PointerMoved?.Invoke(this, e);

            if (IsPointerPositionLocked)
            {
                PointerPosition = capturedPointerPosition;
            }
        }

        protected virtual void OnPointerPressed(PointerEventArgs e)
        {
            PointerPressed?.Invoke(this, e);
        }

        protected virtual void OnPointerReleased(PointerEventArgs e)
        {
            PointerReleased?.Invoke(this, e);
        }

        protected virtual void OnPointerWheelChanged(PointerEventArgs e)
        {
            PointerWheelChanged?.Invoke(this, e);
        }
    }
}
