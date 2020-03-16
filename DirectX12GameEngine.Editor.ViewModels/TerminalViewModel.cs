using System;
using DirectX12GameEngine.Mvvm;
using Microsoft.Build.Framework;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TerminalViewModel : ViewModelBase
    {
        private IStorageFolder currentFolder;
        private string? currentText;

        public TerminalViewModel(IStorageFolder folder, SolutionLoaderViewModel solutionLoader)
        {
            currentFolder = folder;
            SolutionLoader = solutionLoader;

            SolutionLoader.BuildMessageRaised += OnBuildMessageRaised;
        }

        public event AnyEventHandler? BuildMessageRaised;

        public SolutionLoaderViewModel SolutionLoader { get; }

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
            CurrentText += e.Message + Environment.NewLine;

            BuildMessageRaised?.Invoke(sender, e);
        }
    }
}
