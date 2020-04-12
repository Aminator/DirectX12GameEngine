using System;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#nullable enable

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class SolutionExplorerView : UserControl
    {
        public SolutionExplorerView()
        {
            InitializeComponent();
        }

        public SolutionExplorerViewModel ViewModel => (SolutionExplorerViewModel)DataContext;

        private async void OnRefreshContainerRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs e)
        {
            Deferral deferral = e.GetDeferral();

            if (ViewModel.RootFolder != null)
            {
                await ViewModel.RootFolder.FillAsync();
            }

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
