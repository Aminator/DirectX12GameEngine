using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Games
{
    public abstract class GameContext
    {
        public AppContextType ContextType { get; protected set; }

        public int RequestedHeight { get; private protected set; }

        public int RequestedWidth { get; private protected set; }
    }

    public abstract class GameContext<TControl> : GameContext
    {
        public TControl Control { get; private protected set; }

        protected GameContext(TControl control, int requestedWidth = 0, int requestedHeight = 0)
        {
            Control = control;
            RequestedWidth = requestedWidth;
            RequestedHeight = requestedHeight;
        }
    }
}
