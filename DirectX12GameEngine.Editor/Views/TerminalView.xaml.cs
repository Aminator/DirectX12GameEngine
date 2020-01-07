using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class TerminalView : UserControl
    {
        public TerminalView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {

            };
        }

        public TerminalViewModel ViewModel => (TerminalViewModel)DataContext;
    }
}
