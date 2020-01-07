using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DirectX12GameEngine.Editor.Views
{
    public class BackgroundControl : Control
    {
        public BackgroundControl()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            UseSystemFocusVisuals = true;
        }
    }
}
