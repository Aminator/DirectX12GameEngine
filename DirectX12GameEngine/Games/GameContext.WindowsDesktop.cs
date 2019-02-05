#if NETCOREAPP
using System.Windows.Forms;

namespace DirectX12GameEngine.Games
{
    public class GameContextWinForms : GameContext<Control>
    {
        public GameContextWinForms(Control? control = null, int requestedWidth = 0, int requestedHeight = 0)
            : base(control ?? new Form(), requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.WinForms;

            if (requestedHeight == 0 || requestedWidth == 0)
            {
                double resolutionScale = 1.0;

                RequestedWidth = (int)(Control.ClientSize.Width * resolutionScale);
                RequestedHeight = (int)(Control.ClientSize.Height * resolutionScale);
            }
        }
    }
}
#endif
