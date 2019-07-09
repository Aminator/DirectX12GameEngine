using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

#nullable enable

namespace DirectX12GameEngine.Editor
{
    public class ExtendedTabView : TabView
    {
        private const double MinWindowWidth = 192;
        private const double MinWindowHeight = 48;

        private const string TabKey = "Tab";

        public ExtendedTabView()
        {
            AllowDrop = true;
            CanCloseTabs = true;
            CanDragItems = true;
            CanReorderItems = true;

            DragItemsStarting += TabView_DragItemsStarting;
            TabDraggedOutside += TabView_TabDraggedOutside;
        }

        public AppWindow? AppWindow { get; private set; }

        protected override async void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);

            if (AppWindow != null && Items.Count == 0)
            {
                await AppWindow.CloseAsync();
            }
        }

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
                TabView source = (TabView)tab.Parent;
                source.Items.Remove(tab);

                Items.Add(tab);
            }
        }

        private void TabView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            TabView tabView = (TabView)sender;

            object item = e.Items.FirstOrDefault();
            TabViewItem? tab = tabView.ContainerFromItem(item) as TabViewItem;

            if (tab == null && item is FrameworkElement fe)
            {
                tab = fe.FindParent<TabViewItem>();
            }

            if (tab is null)
            {
                for (int i = 0; i < tabView.Items.Count; i++)
                {
                    var tabItem = (TabViewItem)tabView.ContainerFromIndex(i);

                    if (ReferenceEquals(tabItem.Content, item))
                    {
                        tab = tabItem;
                        break;
                    }
                }
            }

            e.Data.Properties.Add(TabKey, tab);
        }

        private async void TabView_TabDraggedOutside(object sender, TabDraggedOutsideEventArgs e)
        {
            Items.Remove(e.Tab);

            await TryCreateNewAppWindowAsync(e.Tab, new Size(ActualWidth, ActualHeight));
        }

        private static async Task<bool> TryCreateNewAppWindowAsync(TabViewItem tab, Size size)
        {
            AppWindow appWindow = await AppWindow.TryCreateAsync();

            //appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            //appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            //appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            appWindow.RequestSize(size);

            WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(MinWindowWidth, MinWindowHeight));

            Grid grid = new Grid();

            ExtendedTabView tabView = new ExtendedTabView { AppWindow = appWindow };

            tabView.Items.Add(tab);
            grid.Children.Add(tabView);

            ElementCompositionPreview.SetAppWindowContent(appWindow, grid);

            return await appWindow.TryShowAsync();
        }
    }
}
