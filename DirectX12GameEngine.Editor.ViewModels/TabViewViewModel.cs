using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Windows.Foundation;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TabViewViewModel : ViewModelBase
    {
        private object? selectedTab;

        public TabViewViewModel()
        {
            CloseCommand = new RelayCommand(() => _ = TryCloseAsync());
            CloseTabCommand = new RelayCommand<object>(tab => _ = TryCloseTabAsync(tab));
        }

        public ObservableCollection<object> Tabs { get; } = new ObservableCollection<object>();

        public RelayCommand CloseCommand { get; }

        public RelayCommand<object> CloseTabCommand { get; }

        public event EventHandler? GotFocus;

        public event EventHandler<TabDroppedOutsideEventArgs>? TabDroppedOutside;

        public object? SelectedTab
        {
            get => selectedTab;
            set => Set(ref selectedTab, value);
        }

        public void Focus()
        {
            GotFocus?.Invoke(this, EventArgs.Empty);
        }

        public void OnTabDroppedOutside(TabDroppedOutsideEventArgs e)
        {
            TabDroppedOutside?.Invoke(this, e);
        }

        public async IAsyncEnumerable<object> GetUnclosableTabsAsync()
        {
            foreach (IEditor closable in Tabs.OfType<IEditor>())
            {
                if (!await closable.TryEditAsync(EditActions.Close))
                {
                    yield return closable;
                }
            }
        }

        public async Task<bool> TryCloseAsync()
        {
            bool canClose = true;

            for (int i = Tabs.Count - 1; i >= 0; i--)
            {
                object tab = Tabs[i];

                if (!(tab is IEditor))
                {
                    Tabs.Remove(tab);
                }
            }

            for (int i = Tabs.Count - 1; i >= 0; i--)
            {
                object tab = Tabs[i];

                if (!await TryCloseTabAsync(tab))
                {
                    canClose = false;
                }
            }

            return canClose;
        }

        public async Task<bool> TryCloseTabAsync(object tab)
        {
            bool canClose = await CanCloseTabAsync(tab);

            if (canClose)
            {
                Tabs.Remove(tab);
            }

            return canClose;
        }

        public async Task<bool> CanCloseTabAsync(object tab)
        {
            return !(tab is IEditor closable) || await closable.TryEditAsync(EditActions.Close);
        }
    }

    public class TabDroppedOutsideEventArgs : EventArgs
    {
        public TabDroppedOutsideEventArgs(object tab, Size windowSize)
        {
            Tab = tab;
            WindowSize = windowSize;
        }

        public object Tab { get; }

        public Size WindowSize { get; }
    }
}
