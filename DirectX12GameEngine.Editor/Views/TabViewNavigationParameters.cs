using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.WindowManagement;

namespace DirectX12GameEngine.Editor.Views
{
    public class TabViewNavigationParameters
    {
        public TabViewNavigationParameters(TabViewItem tab, AppWindow appWindow)
        {
            Tab = tab;
            AppWindow = appWindow;
        }

        public TabViewItem Tab { get; }

        public AppWindow AppWindow { get; }
    }
}
