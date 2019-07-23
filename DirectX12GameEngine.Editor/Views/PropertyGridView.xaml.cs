using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class PropertyGridView : UserControl
    {
        public PropertyGridView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };
        }

        public PropertyGridViewModel ViewModel => (PropertyGridViewModel)DataContext;
    }
}
