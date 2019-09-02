using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class ExtendedTabView : TabView
    {
        private const double MinWindowWidth = 440;
        private const double MinWindowHeight = 48;

        private const string TabKey = "Tab";

        private bool isCreatingNewAppWindow;

        public ExtendedTabView()
        {
            InitializeComponent();

            TabItemsChanged += OnTabItemsChanged;
            TabDragStarting += OnTabDragStarting;
            TabDroppedOutside += OnTabDroppedOutside;
        }

        public AppWindow? AppWindow { get; set; }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            e.AcceptedOperation = DataPackageOperation.Move;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.Properties.TryGetValue(TabKey, out object item) && item is TabViewItem tab)
            {
                TabViewListView source = (TabViewListView)tab.Parent;
                source.Items.Remove(tab);

                TabItems.Add(tab);
            }
        }

        private async void OnTabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs e)
        {
            if (TabItems.Count == 0 && !isCreatingNewAppWindow)
            {
                await TryCloseAsync();
            }
        }

        private void OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs e)
        {
            e.Data.Properties.Add(TabKey, e.Tab);
        }

        private async void OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs e)
        {
            isCreatingNewAppWindow = true;

            TabItems.Remove(e.Tab);

            double scaling = XamlRoot.RasterizationScale;
            await TryCreateNewAppWindowAsync(e.Tab, new Size(ActualWidth * scaling, ActualHeight * scaling));

            if (TabItems.Count == 0)
            {
                await TryCloseAsync();
            }

            isCreatingNewAppWindow = false;
        }

        private async Task<bool> TryCreateNewAppWindowAsync(TabViewItem tab, Size size)
        {
            AppWindow appWindow = await AppWindow.TryCreateAsync();

            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            Frame frame = new Frame();
            frame.Navigate(typeof(AppWindowPage), new TabViewNavigationParameters(tab, appWindow));

            ElementCompositionPreview.SetAppWindowContent(appWindow, frame);

            WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(MinWindowWidth, MinWindowHeight));
            appWindow.RequestSize(size);

            appWindow.RequestMoveAdjacentToCurrentView();

            bool success = await appWindow.TryShowAsync();

            return success;
        }

        private async Task TryCloseAsync()
        {
            if (AppWindow != null)
            {
                TabItemsChanged -= OnTabItemsChanged;
                TabDragStarting -= OnTabDragStarting;
                TabDroppedOutside -= OnTabDroppedOutside;

                await AppWindow.CloseAsync();
            }
        }
    }
}
