using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using DirectX12GameEngine.Mvvm.Messaging;
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

            RegisterMessages();
        }

        public TabViewViewModel MainTabView { get; } = new TabViewViewModel();

        public ProjectLoaderViewModel ProjectLoader { get; } = new ProjectLoaderViewModel();

        public PropertyGridViewModel PropertyGrid { get; } = new PropertyGridViewModel();

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
                        MainTabView.Tabs.Add(PropertyGrid);
                    }
                    else
                    {
                        MainTabView.Tabs.Remove(PropertyGrid);
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
                        MainTabView.Tabs.Add(SolutionExplorer);
                    }
                    else
                    {
                        MainTabView.Tabs.Remove(SolutionExplorer);
                    }
                }
            }
        }

        private void CloseApplication()
        {
            CoreApplication.Exit();
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<ProjectLoadedMessage>(this, m => { IsPropertyGridOpen = true; IsSolutionExplorerOpen = true; });
        }
    }
}
