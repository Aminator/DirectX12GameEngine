using Microsoft.Toolkit.Mvvm.Commands;
using Microsoft.Toolkit.Mvvm.ObjectModel;
using Windows.ApplicationModel.Core;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel(
            SolutionLoaderViewModel solutionLoader,
            SolutionExplorerViewModel solutionExplorer,
            PropertyManagerViewModel propertyManager,
            SdkManagerViewModel sdkManager,
            TabViewManagerViewModel tabViewManager,
            ISolutionLoader solutionLoaderService)
        {
            SolutionLoader = solutionLoader;
            SolutionExplorer = solutionExplorer;
            PropertyManager = propertyManager;
            SdkManager = sdkManager;
            TabViewManager = tabViewManager;

            solutionLoaderService.RootFolderLoaded += (s, e) =>
            {
                TabViewManager.TabViewManager.OpenTab(SolutionExplorer);
                TabViewManager.TabViewManager.TerminalTabView.Tabs.Add(new TerminalViewModel(e.RootFolder, solutionLoaderService));
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
