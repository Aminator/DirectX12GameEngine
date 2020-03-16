using DirectX12GameEngine.Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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

            DataContext = ((App)Application.Current).Locator.Services.GetRequiredService<MainViewModel>();
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string token && !string.IsNullOrEmpty(token))
            {
                await ViewModel.SolutionLoader.OpenRecentSolutionAsync(token);
            }
        }
    }
}
