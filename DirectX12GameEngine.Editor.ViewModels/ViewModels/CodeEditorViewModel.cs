using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class CodeEditorViewModel : ViewModelBase
    {
        private string? text;

        public CodeEditorViewModel()
        {
            SaveCommand = new RelayCommand(async () => await SaveAsync());
        }

        public CodeEditorViewModel(StorageFileViewModel file) : this()
        {
            File = file;
        }

        public StorageFileViewModel? File { get; set; }

        public string? Text
        {
            get => text;
            set => Set(ref text, value);
        }

        public RelayCommand SaveCommand { get; }

        public async Task SaveAsync()
        {
            if (File != null)
            {
                await FileIO.WriteTextAsync(File.Model, Text);
            }
        }
    }
}
