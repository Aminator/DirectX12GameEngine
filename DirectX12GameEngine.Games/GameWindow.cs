using System;
using System.Drawing;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Games
{
    public abstract class GameWindow : IDisposable
    {
        private readonly GameBase game;

        protected GameWindow(GameBase game)
        {
            this.game = game;
            Services = game.Services;
        }

        public bool IsExiting { get; private set; }

        public event EventHandler SizeChanged;

        public abstract Rectangle ClientBounds { get; }

        public abstract WindowHandle NativeWindow { get; }

        internal IServiceProvider Services { get; set; }

        public virtual void Dispose()
        {
        }

        public void Exit()
        {
            IsExiting = true;
            Dispose();
        }

        internal abstract void Run();

        protected virtual void OnSizeChanged(EventArgs e)
        {
            SizeChanged?.Invoke(this, e);
        }

        protected void Tick()
        {
            game.Tick();
        }
    }
}
