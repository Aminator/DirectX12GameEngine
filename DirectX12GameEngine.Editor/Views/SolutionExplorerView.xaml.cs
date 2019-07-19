using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Xaml.Controls;

using WinUI = Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class SolutionExplorerView : UserControl
    {
        public SolutionExplorerView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;
    }
}
