using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppWindowPage : Page
    {
        public AppWindowPage()
        {
            InitializeComponent();

            DataContext = new TabViewViewModel();
            ViewModel.Tabs.CollectionChanged += OnTabsChanged;

            CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;

            UpdateTitleBarLayout(titleBar);

            titleBar.LayoutMetricsChanged += (s, e) => UpdateTitleBarLayout(s);
        }

        public TabViewViewModel ViewModel => (TabViewViewModel)DataContext;

        public AppWindow? AppWindow { get; set; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is TabViewNavigationParameters parameters)
            {
                if (AppWindow != parameters.AppWindow)
                {
                    AppWindow = parameters.AppWindow;
                    AppWindow.CloseRequested += OnAppWindowCloseRequested;
                    AppWindow.Frame.DragRegionVisuals.Add(CustomDragRegion);
                }

                ViewModel.Tabs.Add(parameters.Tab);
            }
        }

        private async void OnTabsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (AppWindow != null && ViewModel.Tabs.Count == 0)
            {
                await AppWindow.CloseAsync();
            }
        }

        private async void OnAppWindowCloseRequested(AppWindow sender, AppWindowCloseRequestedEventArgs e)
        {
            Deferral deferral = e.GetDeferral();

            e.Cancel = !await ViewModel.TryCloseAsync();

            deferral.Complete();
        }

        private async void OpenMainWindow()
        {
            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(ApplicationView.GetApplicationViewIdForWindow(CoreApplication.MainView.CoreWindow));
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar titleBar)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CommandBar.Margin = new Thickness(0, 0, titleBar.SystemOverlayRightInset, 0);
                CustomDragRegion.MinWidth = titleBar.SystemOverlayRightInset;
                ShellTitleBarInset.MinWidth = titleBar.SystemOverlayLeftInset;
            }
            else
            {
                CommandBar.Margin = new Thickness(titleBar.SystemOverlayLeftInset, 0, 0, 0);
                CustomDragRegion.MinWidth = titleBar.SystemOverlayLeftInset;
                ShellTitleBarInset.MinWidth = titleBar.SystemOverlayRightInset;
            }

            CustomDragRegion.Height = ShellTitleBarInset.Height = titleBar.Height;
        }
    }
}
