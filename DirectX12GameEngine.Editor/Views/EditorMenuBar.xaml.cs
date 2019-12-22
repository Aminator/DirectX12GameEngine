using DirectX12GameEngine.Editor.ViewModels;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class EditorMenuBar : WinUI.MenuBar
    {
        public EditorMenuBar()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };

            Loaded += OnEditorMenuBarLoaded;
        }

        private void OnEditorMenuBarLoaded(object sender, RoutedEventArgs e)
        {
            foreach (AccessListEntry accessListEntry in ViewModel.ProjectLoader.RecentProjects)
            {
                MenuFlyoutItem item = new MenuFlyoutItem
                {
                    Text = accessListEntry.Metadata,
                    Command = ViewModel.ProjectLoader.OpenRecentProjectCommand,
                    CommandParameter = accessListEntry.Token
                };

                openRecentFlyoutItem.Items.Add(item);
            }
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;
    }
}
