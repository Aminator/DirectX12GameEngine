using System;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

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

            CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;

            UpdateTitleBarLayout(titleBar);

            titleBar.LayoutMetricsChanged += (s, e) => UpdateTitleBarLayout(s);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is TabViewNavigationParameters parameters)
            {
                tabView.AppWindow = parameters.AppWindow;
                tabView.AppWindow.Frame.DragRegionVisuals.Add(titleBar);

                tabView.Items.Add(parameters.Tab);
            }
        }

        private async void OpenMainWindow()
        {
            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(ApplicationView.GetApplicationViewIdForWindow(CoreApplication.MainView.CoreWindow));
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar coreTitleBar)
        {
            commandBar.Margin = new Thickness(0, 0, coreTitleBar.SystemOverlayRightInset, 0);

            titleBar.Height = coreTitleBar.Height;
        }
    }
}
