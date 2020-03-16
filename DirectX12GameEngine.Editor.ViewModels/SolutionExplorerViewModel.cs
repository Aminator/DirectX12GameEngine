using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels.Factories;
using DirectX12GameEngine.Editor.ViewModels.Properties;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using DirectX12GameEngine.Mvvm.Messaging;
using Windows.System;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionExplorerViewModel : ViewModelBase
    {
        private readonly EditorViewFactory editorViewFactory;
        private StorageFolderViewModel? rootFolder;

        public SolutionExplorerViewModel(EditorViewFactory editorViewFactory)
        {
            this.editorViewFactory = editorViewFactory;

            EngineAssetViewFactory engineAssetViewFactory = new EngineAssetViewFactory();
            engineAssetViewFactory.Add(typeof(Entity), new SceneEditorViewFactory());

            CodeEditorViewFactory codeEditorViewFactory = new CodeEditorViewFactory();

            editorViewFactory.Add(".xaml", engineAssetViewFactory);
            editorViewFactory.Add(".cs", codeEditorViewFactory);
            editorViewFactory.Add(".vb", codeEditorViewFactory);
            editorViewFactory.Add(".csproj", codeEditorViewFactory);
            editorViewFactory.Add(".vbproj", codeEditorViewFactory);

            OpenCommand = new RelayCommand<StorageItemViewModel>(file => _ = OpenAsync(file));
            ViewCodeCommand = new RelayCommand<StorageFileViewModel>(file => _ = ViewCodeAsync(file));
            DeleteCommand = new RelayCommand<StorageItemViewModel>(Delete);
            ShowPropertiesCommand = new RelayCommand<StorageItemViewModel>(ShowProperties);

            RefreshCommand = new RelayCommand(() => _ = RefreshAsync(), () => RootFolder != null);
        }

        public TabViewViewModel MainTabView { get; } = new TabViewViewModel();

        public StorageFolderViewModel? RootFolder
        {
            get => rootFolder;
            set => Set(ref rootFolder, value);
        }

        public RelayCommand<StorageItemViewModel> OpenCommand { get; }

        public RelayCommand<StorageFileViewModel> ViewCodeCommand { get; }

        public RelayCommand<StorageItemViewModel> DeleteCommand { get; }

        public RelayCommand<StorageItemViewModel> ShowPropertiesCommand { get; }

        public RelayCommand RefreshCommand { get; }

        public async Task OpenAsync(StorageItemViewModel item)
        {
            if (item is StorageFileViewModel file)
            {
                object? editor = await editorViewFactory.CreateAsync(file.Model);

                if (editor != null)
                {
                    MainTabView.Tabs.Add(editor);
                }
                else
                {
                    await Launcher.LaunchFileAsync(file.Model);
                }
            }
            else if (item is StorageFolderViewModel folder)
            {
                await Launcher.LaunchFolderAsync(folder.Model);
            }
        }

        public async Task ViewCodeAsync(StorageFileViewModel file)
        {
            object? editor = await new CodeEditorViewFactory().CreateAsync(file.Model, editorViewFactory.Services);

            if (editor != null)
            {
                MainTabView.Tabs.Add(editor);
            }
            else
            {
                await Launcher.LaunchFileAsync(file.Model);
            }
        }

        public void Delete(StorageItemViewModel item)
        {
            item.Parent?.Children.Remove(item);
        }

        public void ShowProperties(StorageItemViewModel item)
        {
            EventBus.Default.Publish(this, new PropertiesViewRequestedEventArgs(item.Model));
        }

        public async Task RefreshAsync()
        {
            if (RootFolder != null)
            {
                RootFolder = new StorageFolderViewModel(RootFolder.Model);
                await RootFolder.FillAsync();
                RootFolder.IsExpanded = true;
            }
        }
    }
}
