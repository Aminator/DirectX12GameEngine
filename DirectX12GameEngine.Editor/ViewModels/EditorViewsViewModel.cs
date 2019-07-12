namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EditorViewsViewModel : ViewModelBase
    {
        private bool isSolutionExplorerOpen = true;
        private bool isPropertyGridOpen;

        public bool IsSolutionExplorerOpen
        {
            get => isSolutionExplorerOpen;
            set => Set(ref isSolutionExplorerOpen, value);
        }

        public bool IsPropertyGridOpen
        {
            get => isPropertyGridOpen;
            set => Set(ref isPropertyGridOpen, value);
        }
    }
}
