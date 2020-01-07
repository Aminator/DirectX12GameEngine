using DirectX12GameEngine.Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class EditorTabView : TabView
    {
        private const double MinWindowWidth = 440;
        private const double MinWindowHeight = 48;

        private const string TabKey = "Tab";
        private const string TabViewKey = "TabView";

        public EditorTabView()
        {
            InitializeComponent();

            TabItemsChanged += OnTabItemsChanged;
            //TabStripDragOver += OnTabStripDragOver;
            TabDragStarting += OnTabDragStarting;
            TabDroppedOutside += OnTabDroppedOutside;
            TabCloseRequested += OnTabCloseRequested;
        }

        public TabViewViewModel ViewModel => (TabViewViewModel)DataContext;

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            e.AcceptedOperation = DataPackageOperation.Move;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.Properties.TryGetValue(TabKey, out object tab)
                && e.Data.Properties.TryGetValue(TabViewKey, out object tabView) && tabView is TabViewViewModel originalTabView)
            {
                originalTabView.Tabs.Remove(tab);
                ViewModel.Tabs.Add(tab);
            }
        }

        private void OnTabItemsChanged(TabView sender, IVectorChangedEventArgs e)
        {
            if (e.CollectionChange == CollectionChange.ItemInserted)
            {
                SelectedIndex = (int)e.Index;
            }
        }

        //private void OnTabStripDragOver(object sender, DragEventArgs e)
        //{
        //    e.AcceptedOperation = DataPackageOperation.Move;
        //}

        private void OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs e)
        {
            e.Data.Properties.Add(TabKey, e.Item);
            e.Data.Properties.Add(TabViewKey, ViewModel);
        }

        private async void OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs e)
        {
            ViewModel.Tabs.Remove(e.Item);

            double scaling = XamlRoot.RasterizationScale;
            await TryCreateNewAppWindowAsync(e.Item, new Size(ActualWidth * scaling, ActualHeight * scaling));
        }

        private async void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs e)
        {
            await ViewModel.TryCloseTabAsync(e.Item);
        }

        private async Task<bool> TryCreateNewAppWindowAsync(object tab, Size size)
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
    }
}
