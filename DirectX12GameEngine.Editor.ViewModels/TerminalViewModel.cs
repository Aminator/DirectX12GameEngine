using Microsoft.Toolkit.Mvvm.ObjectModel;
using Microsoft.Build.Framework;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TerminalViewModel : ObservableObject
    {
        private IStorageFolder currentFolder;
        private string? currentText;

        public TerminalViewModel(IStorageFolder folder, ISolutionLoader solutionLoader)
        {
            currentFolder = folder;
            SolutionLoader = solutionLoader;

            SolutionLoader.BuildMessageRaised += OnBuildMessageRaised;
        }

        public event AnyEventHandler? BuildMessageRaised;

        public ISolutionLoader SolutionLoader { get; }

        public IStorageFolder CurrentFolder
        {
            get => currentFolder;
            set => Set(ref currentFolder, value);
        }

        public string? CurrentText
        {
            get => currentText;
            set => Set(ref currentText, value);
        }

        private void OnBuildMessageRaised(object sender, BuildEventArgs e)
        {
            //CurrentText += e.Message + Environment.NewLine;

            BuildMessageRaised?.Invoke(sender, e);
        }
    }
}
