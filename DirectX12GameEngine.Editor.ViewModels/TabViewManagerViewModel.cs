using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.Toolkit.Mvvm.Commands;
using Microsoft.Toolkit.Mvvm.ObjectModel;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TabViewManagerViewModel : ObservableObject
    {
        private readonly IWindowManager windowManager;

        private object? selectedTab;

        public TabViewManagerViewModel(ITabViewManager tabViewManager, IWindowManager windowManager)
        {
            TabViewManager = tabViewManager;
            this.windowManager = windowManager;

            OnTabViewAdded(TabViewManager.MainTabView);
            OnTabViewAdded(TabViewManager.SolutionExplorerTabView);
            OnTabViewAdded(TabViewManager.TerminalTabView);

            TabViewManager.TabViews.CollectionChanged += OnTabViewsCollectionChanged;

            SaveCommand = new RelayCommand(() => _ = (SelectedTab as IEditor)?.TryEditAsync(EditActions.Save), () => (SelectedTab as IEditor)?.SupportsAction(EditActions.Save) ?? false);
            OpenTabCommand = new RelayCommand<object>(tabViewManager.OpenTab);
            CloseTabCommand = new RelayCommand<object>(tabViewManager.CloseTab);
        }

        public ITabViewManager TabViewManager { get; }

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

        private void OnTabViewsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (TabViewViewModel tabView in e.NewItems.Cast<TabViewViewModel>())
                    {
                        OnTabViewAdded(tabView);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (TabViewViewModel tabView in e.NewItems.Cast<TabViewViewModel>())
                    {
                        OnTabViewRemoved(tabView);
                    }
                    break;
            }
        }

        private void OnTabViewAdded(TabViewViewModel tabView)
        {
            tabView.TabDroppedOutside += OnTabViewTabDroppedOutside;
            tabView.GotFocus += OnTabViewGotFocus;
            tabView.PropertyChanged += OnTabViewPropertyChanged;
        }

        private void OnTabViewRemoved(TabViewViewModel tabView)
        {
            tabView.TabDroppedOutside -= OnTabViewTabDroppedOutside;
            tabView.GotFocus -= OnTabViewGotFocus;
            tabView.PropertyChanged -= OnTabViewPropertyChanged;
        }

        private async void OnTabViewTabDroppedOutside(object sender, TabDroppedOutsideEventArgs e)
        {
            TabViewViewModel tabView = new TabViewViewModel();
            tabView.Tabs.Add(e.Tab);
            tabView.SelectedTab = e.Tab;
            TabViewManager.TabViews.Add(tabView);

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
