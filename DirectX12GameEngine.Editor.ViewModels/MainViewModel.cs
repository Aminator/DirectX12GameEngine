using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Windows.ApplicationModel.Core;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool isSolutionExplorerOpen;
        private bool isPropertiesViewOpen;
        private bool isSdkManagerOpen;

        public MainViewModel(
            SolutionLoaderViewModel solutionLoader,
            SolutionExplorerViewModel solutionExplorer,
            PropertiesViewModel propertiesView,
            SdkManagerViewModel sdkManager)
        {
            SolutionLoader = solutionLoader;
            SolutionExplorer = solutionExplorer;
            PropertiesView = propertiesView;
            SdkManager = sdkManager;

            SolutionLoader.RootFolderLoaded += (s, e) =>
            {
                SolutionExplorer.RootFolder = new StorageFolderViewModel(e.RootFolder)
                {
                    IsExpanded = true
                };

                IsSolutionExplorerOpen = true;

                TerminalTabView.Tabs.Add(new TerminalViewModel(e.RootFolder, SolutionLoader));
            };

            CloseApplicationCommand = new RelayCommand(CloseApplication);
        }

        public SolutionLoaderViewModel SolutionLoader { get; }

        public SolutionExplorerViewModel SolutionExplorer { get; }

        public PropertiesViewModel PropertiesView { get; }

        public SdkManagerViewModel SdkManager { get; }

        public TabViewViewModel SolutionExplorerTabView { get; } = new TabViewViewModel();

        public TabViewViewModel TerminalTabView { get; } = new TabViewViewModel();

        public bool IsSolutionExplorerOpen
        {
            get => isSolutionExplorerOpen;
            set
            {
                if (Set(ref isSolutionExplorerOpen, value))
                {
                    if (isSolutionExplorerOpen)
                    {
                        SolutionExplorerTabView.Tabs.Add(SolutionExplorer);
                    }
                    else
                    {
                        SolutionExplorerTabView.Tabs.Remove(SolutionExplorer);
                    }
                }
            }
        }

        public bool IsPropertiesViewOpen
        {
            get => isPropertiesViewOpen;
            set
            {
                if (Set(ref isPropertiesViewOpen, value))
                {
                    if (isPropertiesViewOpen)
                    {
                        SolutionExplorerTabView.Tabs.Add(PropertiesView);
                    }
                    else
                    {
                        SolutionExplorerTabView.Tabs.Remove(PropertiesView);
                    }
                }
            }
        }

        public bool IsSdkManagerOpen
        {
            get => isSdkManagerOpen;
            set
            {
                if (Set(ref isSdkManagerOpen, value))
                {
                    if (isSdkManagerOpen)
                    {
                        SolutionExplorerTabView.Tabs.Add(SdkManager);
                    }
                    else
                    {
                        SolutionExplorerTabView.Tabs.Remove(SdkManager);
                    }
                }
            }
        }

        public RelayCommand CloseApplicationCommand { get; }

        private void CloseApplication()
        {
            CoreApplication.Exit();
        }
    }
}
