using System;
using System.Drawing;

namespace DirectX12GameEngine.Games
{
    public abstract class GameWindow
    {
        public event EventHandler? SizeChanged;

        public event EventHandler? TickRequested;

        public bool IsExiting { get; private set; }

        public abstract RectangleF ClientBounds { get; }

        public void Exit()
        {
            IsExiting = true;
        }

        public abstract void Run();

        protected virtual void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void Tick()
        {
            TickRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
