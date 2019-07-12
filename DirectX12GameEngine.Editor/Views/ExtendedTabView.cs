using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public class ExtendedTabView : TabView
    {
        private const double MinWindowWidth = 192;
        private const double MinWindowHeight = 48;

        private const string TabKey = "Tab";

        private bool isDragging;
        private bool isCreatingNewAppWindow;

        public ExtendedTabView()
        {
            AllowDrop = true;
            CanCloseTabs = true;
            CanDragItems = true;
            CanReorderItems = true;

            DragItemsStarting += TabView_DragItemsStarting;
            DragItemsCompleted += ExtendedTabView_DragItemsCompleted;
            TabDraggedOutside += TabView_TabDraggedOutside;
        }

        public AppWindow? AppWindow { get; private set; }

        protected override async void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);

            await CloseAsync();
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            e.AcceptedOperation = DataPackageOperation.Move;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (!isDragging && e.Data.Properties.TryGetValue(TabKey, out object item) && item is TabViewItem tab)
            {
                ExtendedTabView source = (ExtendedTabView)tab.Parent;
                source.Items.Remove(tab);

                int selectedIndex = SelectedIndex;
                Items.Add(tab);
                SelectedIndex = selectedIndex;
            }
        }

        private async void TabView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            isDragging = true;

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

            //StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("TabViewItem.png", CreationCollisionOption.ReplaceExisting);

            //RenderTargetBitmap bitmap = new RenderTargetBitmap();
            //await bitmap.RenderAsync(tab);
            //IBuffer pixels = await bitmap.GetPixelsAsync();

            //BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, await file.OpenAsync(FileAccessMode.ReadWrite));
            //SoftwareBitmap softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(pixels, BitmapPixelFormat.Bgra8, bitmap.PixelWidth, bitmap.PixelHeight);
            //encoder.SetSoftwareBitmap(softwareBitmap);
            //await encoder.FlushAsync();

            //e.Data.SetStorageItems(new[] { file });
        }

        private void ExtendedTabView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            isDragging = false;
        }

        private async void TabView_TabDraggedOutside(object sender, TabDraggedOutsideEventArgs e)
        {
            isCreatingNewAppWindow = true;
            Items.Remove(e.Tab);

            double scaling = XamlRoot.RasterizationScale;
            await TryCreateNewAppWindowAsync(e.Tab, new Size(ActualWidth * scaling, ActualHeight * scaling));

            isCreatingNewAppWindow = false;
            await CloseAsync();
        }

        private async Task<bool> TryCreateNewAppWindowAsync(TabViewItem tab, Size size)
        {
            AppWindow appWindow = await AppWindow.TryCreateAsync();

            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(MinWindowWidth, MinWindowHeight));

            appWindow.RequestSize(size);

            Grid grid = new Grid { Background = (Brush)Resources["SystemControlAcrylicWindowBrush"] };

            Grid titleBar = new Grid { Height = 40, VerticalAlignment = VerticalAlignment.Top, Background = new SolidColorBrush(Colors.Transparent) };
            grid.Children.Add(titleBar);

            ExtendedTabView tabView = new ExtendedTabView { AppWindow = appWindow };
            tabView.Items.Add(tab);
            grid.Children.Add(tabView);

            appWindow.Frame.DragRegionVisuals.Add(titleBar);

            ElementCompositionPreview.SetAppWindowContent(appWindow, grid);

            bool success = await appWindow.TryShowAsync();

            return success;
        }

        private async Task CloseAsync()
        {
            if (Items.Count == 0 && !isCreatingNewAppWindow)
            {
                DragItemsStarting -= TabView_DragItemsStarting;
                DragItemsCompleted -= ExtendedTabView_DragItemsCompleted;
                TabDraggedOutside -= TabView_TabDraggedOutside;

                if (AppWindow != null)
                {
                    await AppWindow.CloseAsync();
                }
            }
        }
    }
}
