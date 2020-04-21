using System.Collections.ObjectModel;
using System.Linq;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TabViewManager : ITabViewManager
    {
        public TabViewManager()
        {
            TabViews.Add(MainTabView);
            TabViews.Add(SolutionExplorerTabView);
            TabViews.Add(TerminalTabView);
        }

        public ObservableCollection<TabViewViewModel> TabViews { get; } = new ObservableCollection<TabViewViewModel>();

        public TabViewViewModel MainTabView { get; } = new TabViewViewModel();

        public TabViewViewModel SolutionExplorerTabView { get; } = new TabViewViewModel();

        public TabViewViewModel TerminalTabView { get; } = new TabViewViewModel();

        public void OpenTab(object tab)
        {
            OpenTab(tab, SolutionExplorerTabView);
        }

        public void OpenTab(object tab, TabViewViewModel tabView)
        {
            TabViewViewModel existingTabView = TabViews.FirstOrDefault(t => t.Tabs.Any(t => t == tab));

            if (existingTabView is null)
            {
                tabView.Tabs.Add(tab);
            }
            else
            {
                tabView = existingTabView;
            }

            tabView.SelectedTab = tab;
        }

        public void CloseTab(object tab)
        {
            foreach (TabViewViewModel tabView in TabViews)
            {
                tabView.Tabs.Remove(tab);
            }
        }
    }
}
