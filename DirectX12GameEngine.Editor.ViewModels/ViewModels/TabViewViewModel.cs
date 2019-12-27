using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TabViewViewModel : ViewModelBase
    {
        private int selectedIndex;

        public TabViewViewModel()
        {
            CloseCommand = new RelayCommand(async () => await TryCloseAsync());
            CloseTabCommand = new RelayCommand<object>(async tab => await TryCloseTabAsync(tab));
        }

        public ObservableCollection<object> Tabs { get; } = new ObservableCollection<object>();

        public RelayCommand CloseCommand { get; }

        public RelayCommand<object> CloseTabCommand { get; }

        public int SelectedIndex
        {
            get => selectedIndex;
            set => Set(ref selectedIndex, value);
        }

        public async IAsyncEnumerable<object> GetUnclosableTabsAsync()
        {
            foreach (IClosable closable in Tabs.OfType<IClosable>())
            {
                if (!closable.CanClose && !await closable.TryCloseAsync())
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

                if (!(tab is IClosable closable) || closable.CanClose)
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
            return !(tab is IClosable closable) || closable.CanClose || await closable.TryCloseAsync();
        }
    }
}
