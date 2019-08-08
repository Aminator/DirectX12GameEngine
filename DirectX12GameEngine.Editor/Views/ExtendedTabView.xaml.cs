using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
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

            DragItemsStarting += TabView_DragItemsStarting;
            TabDraggedOutside += TabView_TabDraggedOutside;
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
                ExtendedTabView source = (ExtendedTabView)tab.Parent;
                source.Items.Remove(tab);

                int previousSelectedIndex = SelectedIndex;
                Items.Add(tab);
                SelectedIndex = previousSelectedIndex;
            }
        }

        protected override async void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);

            if (Items.Count == 0 && !isCreatingNewAppWindow)
            {
                await TryCloseAsync();
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

        private async void TabView_TabDraggedOutside(object sender, TabDraggedOutsideEventArgs e)
        {
            isCreatingNewAppWindow = true;

            Items.Remove(e.Tab);

            double scaling = XamlRoot.RasterizationScale;
            await TryCreateNewAppWindowAsync(e.Tab, new Size(ActualWidth * scaling, ActualHeight * scaling));

            if (Items.Count == 0)
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
                DragItemsStarting -= TabView_DragItemsStarting;
                TabDraggedOutside -= TabView_TabDraggedOutside;

                await AppWindow.CloseAsync();
            }
        }
    }
}
