#if NETCOREAPP
using System.Windows.Forms;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Games
{
    public class GameContextWinForms : GameContext<Control>
    {
        public GameContextWinForms(Control? control = null, int requestedWidth = 0, int requestedHeight = 0)
            : base(control ?? new Form(), requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.WinForms;
        }

        public override GameWindow CreateWindow(GameBase game) => new GameWindowWinForms(game, this);
    }
}
#endif
