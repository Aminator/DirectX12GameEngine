using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Windows.ApplicationModel.Core;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool isPropertyGridOpen;
        private bool isSolutionExplorerOpen;

        public MainViewModel()
        {
            CloseApplicationCommand = new RelayCommand(CloseApplication);

            ProjectLoader.ProjectLoaded += (s, e) =>
            {
                SolutionExplorer.RootFolder = e.RootFolder;
                SolutionExplorer.RootFolder.IsExpanded = true;

                IsPropertyGridOpen = true;
                IsSolutionExplorerOpen = true;

                TerminalTabView.Tabs.Add(new TerminalViewModel(e.RootFolder));
            };
        }

        public TabViewViewModel SolutionExplorerTabView { get; } = new TabViewViewModel();

        public TabViewViewModel TerminalTabView { get; } = new TabViewViewModel();

        public ProjectLoaderViewModel ProjectLoader { get; } = new ProjectLoaderViewModel();

        public PropertiesViewModel PropertyGrid { get; } = new PropertiesViewModel();

        public SolutionExplorerViewModel SolutionExplorer { get; } = new SolutionExplorerViewModel();

        public RelayCommand CloseApplicationCommand { get; }

        public bool IsPropertyGridOpen
        {
            get => isPropertyGridOpen;
            set
            {
                if (Set(ref isPropertyGridOpen, value))
                {
                    if (isPropertyGridOpen)
                    {
                        SolutionExplorerTabView.Tabs.Add(PropertyGrid);
                    }
                    else
                    {
                        SolutionExplorerTabView.Tabs.Remove(PropertyGrid);
                    }
                }
            }
        }

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

        private void CloseApplication()
        {
            CoreApplication.Exit();
        }
    }
}
