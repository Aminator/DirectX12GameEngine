using Windows.UI.WindowManagement;

namespace DirectX12GameEngine.Editor.Views
{
    public class TabViewNavigationParameters
    {
        public TabViewNavigationParameters(object tab, AppWindow appWindow)
        {
            Tab = tab;
            AppWindow = appWindow;
        }

        public object Tab { get; }

        public AppWindow AppWindow { get; }
    }
}
