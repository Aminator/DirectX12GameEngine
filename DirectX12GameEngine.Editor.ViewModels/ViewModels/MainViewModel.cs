using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Windows.ApplicationModel.Core;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            CloseApplicationCommand = new RelayCommand(CloseApplication);

            RegisterMessages();
        }

        public EditorViewsViewModel EditorViews { get; } = new EditorViewsViewModel();

        public ProjectLoaderViewModel ProjectLoader { get; } = new ProjectLoaderViewModel();

        public PropertyGridViewModel PropertyGrid { get; } = new PropertyGridViewModel();

        public SolutionExplorerViewModel SolutionExplorer { get; } = new SolutionExplorerViewModel();

        public RelayCommand CloseApplicationCommand { get; }

        private void CloseApplication()
        {
            CoreApplication.Exit();
        }

        private void RegisterMessages()
        {
        }
    }
}
