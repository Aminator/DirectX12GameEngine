#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            RegisterMessages();
        }

        public EditorViewsViewModel EditorViews { get; } = new EditorViewsViewModel();

        public ProjectLoaderViewModel ProjectLoader { get; } = new ProjectLoaderViewModel();

        public PropertyGridViewModel PropertyGrid { get; } = new PropertyGridViewModel();

        public SolutionExplorerViewModel SolutionExplorer { get; } = new SolutionExplorerViewModel();

        private void RegisterMessages()
        {
        }
    }
}
