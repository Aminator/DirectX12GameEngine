using System.Diagnostics;
using System.IO;
using DirectX12GameEngine.Mvvm;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class TerminalViewModel : ViewModelBase
    {
        private StorageFolderViewModel currentFolder;

        public TerminalViewModel(StorageFolderViewModel rootFolder)
        {
            currentFolder = rootFolder;
        }

        public StorageFolderViewModel CurrentFolder
        {
            get => currentFolder;
            set => Set(ref currentFolder, value);
        }

        public Process? CurrentProcess { get; set; }

        public StreamWriter StandardInput { get; set; } = new StreamWriter(new MemoryStream());

        public StreamReader StandardOutput { get; set; } = new StreamReader(new MemoryStream());
    }
}
