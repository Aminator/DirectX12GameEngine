using Windows.UI.WindowManagement;
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
    }
}
