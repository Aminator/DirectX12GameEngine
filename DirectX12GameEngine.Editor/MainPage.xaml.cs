using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Editor.Views;
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
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is MainPageNavigationParameters parameters)
            {
                DataContext = parameters.ViewModel;

                if (!string.IsNullOrEmpty(parameters.Arguments))
                {
                    ViewModel.SolutionLoader.OpenRecentSolutionCommand.Execute(parameters.Arguments);
                }
            }
        }
    }
}
