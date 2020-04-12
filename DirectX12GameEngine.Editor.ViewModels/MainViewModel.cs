using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Windows.ApplicationModel.Core;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            SolutionLoaderViewModel solutionLoader,
            SolutionExplorerViewModel solutionExplorer,
            PropertyManagerViewModel propertyManager,
            SdkManagerViewModel sdkManager,
            TabViewManagerViewModel tabViewManager)
        {
            SolutionLoader = solutionLoader;
            SolutionExplorer = solutionExplorer;
            PropertyManager = propertyManager;
            SdkManager = sdkManager;
            TabViewManager = tabViewManager;

            SolutionLoader.RootFolderLoaded += (s, e) =>
            {
                TabViewManager.OpenTab(SolutionExplorer);
                TabViewManager.TerminalTabView.Tabs.Add(new TerminalViewModel(e.RootFolder, SolutionLoader));
            };

            CloseApplicationCommand = new RelayCommand(CloseApplication);
        }

        public SolutionLoaderViewModel SolutionLoader { get; }

        public SolutionExplorerViewModel SolutionExplorer { get; }

        public PropertyManagerViewModel PropertyManager { get; }

        public SdkManagerViewModel SdkManager { get; }

        public TabViewManagerViewModel TabViewManager { get; }

        public RelayCommand CloseApplicationCommand { get; }

        private void CloseApplication()
        {
            CoreApplication.Exit();
        }
    }
}
