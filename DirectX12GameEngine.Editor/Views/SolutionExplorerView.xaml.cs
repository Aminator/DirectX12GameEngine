using DirectX12GameEngine.Editor.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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

            ((StandardUICommand)Resources["OpenCommand"]).KeyboardAccelerators.Clear();
        }

        public SolutionExplorerViewModel ViewModel => (SolutionExplorerViewModel)DataContext;

        private void RefreshContainer_RefreshRequested(Microsoft.UI.Xaml.Controls.RefreshContainer sender, Microsoft.UI.Xaml.Controls.RefreshRequestedEventArgs args)
        {
            Deferral deferral = args.GetDeferral();

            ViewModel.RefreshCommand.Execute(null);

            deferral.Complete();
        }
    }
}
