using System.Collections.ObjectModel;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public interface ITabViewManager
    {
        ObservableCollection<TabViewViewModel> TabViews { get; }

        TabViewViewModel MainTabView { get; }

        TabViewViewModel SolutionExplorerTabView { get; }

        TabViewViewModel TerminalTabView { get; }

        void OpenTab(object tab);

        void OpenTab(object tab, TabViewViewModel tabView);

        void CloseTab(object tab);
    }
}
