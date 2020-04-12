using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.WindowManagement;

namespace DirectX12GameEngine.Editor.Views
{
    public class TabViewNavigationParameters
    {
        public TabViewNavigationParameters(TabViewViewModel tabView, AppWindow appWindow)
        {
            TabView = tabView;
            AppWindow = appWindow;
        }

        public TabViewViewModel TabView { get; }

        public AppWindow AppWindow { get; }
    }
}
