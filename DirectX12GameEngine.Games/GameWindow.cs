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

        public static GameWindow Create(GameBase game) => game.Context.ContextType switch
        {
#if WINDOWS_UWP
            AppContextType.CoreWindow => new GameWindowUwp(game),
            AppContextType.Xaml => new GameWindowUwp(game),
#endif
#if NETCOREAPP
            AppContextType.WinForms => new GameWindowWinForms(game),
#endif
            _ => throw new PlatformNotSupportedException("This context is not supported on this platform.")
        };

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
