using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace DirectX12GameEngine.Editor.Views
{
    public class BackgroundControl : ButtonBase
    {
        public BackgroundControl()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            UseSystemFocusVisuals = true;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);

            e.Handled = false;
        }
    }
}
