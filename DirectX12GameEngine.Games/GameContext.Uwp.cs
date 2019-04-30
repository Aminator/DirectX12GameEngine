#if WINDOWS_UWP
using DirectX12GameEngine.Core;
using Windows.Graphics.Holographic;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace DirectX12GameEngine.Games
{
    public class GameContextCoreWindow : GameContext<CoreWindow>
    {
        public GameContextCoreWindow(CoreWindow? control = null, int requestedWidth = 0, int requestedHeight = 0)
            : base(control ?? CoreWindow.GetForCurrentThread(), requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.CoreWindow;
        }
    }

    public class GameContextHolographic : GameContextCoreWindow
    {
        public GameContextHolographic(HolographicSpace? holographicSpace = null, CoreWindow? control = null, int requestedWidth = 0, int requestedHeight = 0)
            : base(control, requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.Holographic;

            HolographicSpace = holographicSpace ?? HolographicSpace.CreateForCoreWindow(Control);
        }

        public HolographicSpace HolographicSpace { get; }
    }

    public class GameContextXaml : GameContext<SwapChainPanel>
    {
        public GameContextXaml(SwapChainPanel control, int requestedWidth = 0, int requestedHeight = 0)
            : base(control, requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.Xaml;
        }
    }
}
#endif
