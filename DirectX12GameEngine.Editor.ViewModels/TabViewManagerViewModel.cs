using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TabViewManagerViewModel : ViewModelBase
    {
        private readonly IWindowManager windowManager;

        private object? selectedTab;

        public TabViewManagerViewModel(IWindowManager windowManager)
        {
            this.windowManager = windowManager;

            TabViews.CollectionChanged += OnTabViewsCollectionChanged;

            TabViews.Add(MainTabView);
            TabViews.Add(SolutionExplorerTabView);
            TabViews.Add(TerminalTabView);

            SaveCommand = new RelayCommand(() => _ = (SelectedTab as IEditor)?.TryEditAsync(EditActions.Save), () => (SelectedTab as IEditor)?.SupportsAction(EditActions.Save) ?? false);
            OpenTabCommand = new RelayCommand<object>(OpenTab);
            CloseTabCommand = new RelayCommand<object>(CloseTab);
        }

        public ObservableCollection<TabViewViewModel> TabViews { get; } = new ObservableCollection<TabViewViewModel>();

        public TabViewViewModel MainTabView { get; } = new TabViewViewModel();

        public TabViewViewModel SolutionExplorerTabView { get; } = new TabViewViewModel();

        public TabViewViewModel TerminalTabView { get; } = new TabViewViewModel();

        public RelayCommand SaveCommand { get; }

        public RelayCommand<object> OpenTabCommand { get; }

        public RelayCommand<object> CloseTabCommand { get; }

        public object? SelectedTab
        {
            get => selectedTab;
            set
            {
                if (Set(ref selectedTab, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
                }
            }
        }

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

        private void OnTabViewsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (TabViewViewModel tabView in e.NewItems.Cast<TabViewViewModel>())
                    {
                        tabView.TabDroppedOutside += OnTabViewTabDroppedOutside;
                        tabView.GotFocus += OnTabViewGotFocus;
                        tabView.PropertyChanged += OnTabViewPropertyChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (TabViewViewModel tabView in e.NewItems.Cast<TabViewViewModel>())
                    {
                        tabView.TabDroppedOutside -= OnTabViewTabDroppedOutside;
                        tabView.GotFocus -= OnTabViewGotFocus;
                        tabView.PropertyChanged -= OnTabViewPropertyChanged;
                    }
                    break;
            }
        }

        private async void OnTabViewTabDroppedOutside(object sender, TabDroppedOutsideEventArgs e)
        {
            TabViewViewModel tabView = new TabViewViewModel();
            tabView.Tabs.Add(e.Tab);
            TabViews.Add(tabView);

            await windowManager.TryCreateNewWindowAsync(tabView, e.WindowSize);
        }

        private void OnTabViewGotFocus(object sender, EventArgs e)
        {
            object? newSelectedTab = ((TabViewViewModel)sender).SelectedTab;

            if (newSelectedTab != null)
            {
                SelectedTab = newSelectedTab;
            }
        }

        private void OnTabViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TabViewViewModel.SelectedTab))
            {
                SelectedTab = ((TabViewViewModel)sender).SelectedTab;
            }
        }
    }
}
