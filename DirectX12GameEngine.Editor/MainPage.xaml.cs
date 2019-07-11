using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using WinUI = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

#nullable enable

namespace DirectX12GameEngine.Editor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            Window.Current.SetTitleBar(titleBar);
        }

        public ProjectLoader ProjectLoader { get; } = new ProjectLoader();

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string token && !string.IsNullOrEmpty(token))
            {
                await ProjectLoader.OpenRecentProjectAsync(token);
            }
        }

        private void SolutionExplorer_Collapsed(WinUI.TreeView sender, WinUI.TreeViewCollapsedEventArgs args)
        {
            if (args.Item is StorageItemViewModel item)
            {
                item.Collapse();
            }
        }

        private async void SolutionExplorer_Expanding(WinUI.TreeView sender, WinUI.TreeViewExpandingEventArgs args)
        {
            if (args.Item is StorageItemViewModel item)
            {
                await item.ExpandAsync();
            }
        }

        private void SolutionExplorer_ItemInvoked(WinUI.TreeView sender, WinUI.TreeViewItemInvokedEventArgs args)
        {
            propertyListView.Items.Clear();

            if (args.InvokedItem is StorageItemViewModel item)
            {
                TextBlock textBlock = new TextBlock { Text = item.Name };
                propertyListView.Items.Add(textBlock);
            }
        }
    }
}
