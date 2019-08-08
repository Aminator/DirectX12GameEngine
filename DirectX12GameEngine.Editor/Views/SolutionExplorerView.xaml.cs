using System;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

#nullable enable

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

        private async void RefreshContainer_RefreshRequested(Microsoft.UI.Xaml.Controls.RefreshContainer sender, Microsoft.UI.Xaml.Controls.RefreshRequestedEventArgs e)
        {
            Deferral deferral = e.GetDeferral();

            await ViewModel.RefreshAsync();

            deferral.Complete();
        }
    }

    public class StorageItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? FileTemplate { get; set; }

        public DataTemplate? FolderTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            DataTemplate? template = item is StorageFileViewModel ? FileTemplate : FolderTemplate;

            return template ?? throw new InvalidOperationException();
        }
    }
}
