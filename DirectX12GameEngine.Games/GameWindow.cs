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

        public static GameWindow Create(GameBase game)
        {
#if WINDOWS_UWP
            return new GameWindowUwp(game);
#elif NETCOREAPP
            return new GameWindowWinForms(game);
#endif
        }

        public event EventHandler SizeChanged;

        public abstract Rectangle ClientBounds { get; }

        public abstract WindowHandle NativeWindow { get; }

        internal IServiceProvider Services { get; set; }

        public virtual void Dispose()
        {
        }

        internal abstract void Run();

        protected void OnSizeChanged(object sender, EventArgs e)
        {
            SizeChanged?.Invoke(sender, e);
        }

        protected void Tick()
        {
            game.Tick();
        }
    }
}
