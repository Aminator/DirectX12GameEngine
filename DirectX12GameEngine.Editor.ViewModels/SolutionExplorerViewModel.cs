using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels.Factories;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionExplorerViewModel : ViewModelBase
    {
        private readonly SolutionLoaderViewModel solutionLoader;
        private readonly EditorViewFactory editorViewFactory;
        private readonly PropertyManagerViewModel propertyManager;
        private readonly TabViewManagerViewModel tabViewManager;

        private StorageFolderViewModel? rootFolder;

        public SolutionExplorerViewModel(SolutionLoaderViewModel solutionLoader, EditorViewFactory editorViewFactory, PropertyManagerViewModel propertyManager, TabViewManagerViewModel tabViewManager)
        {
            this.solutionLoader = solutionLoader;
            this.editorViewFactory = editorViewFactory;
            this.propertyManager = propertyManager;
            this.tabViewManager = tabViewManager;

            if (solutionLoader.RootFolder != null)
            {
                RootFolder = new StorageFolderViewModel(solutionLoader.RootFolder)
                {
                    IsExpanded = true
                };
            }

            solutionLoader.RootFolderLoaded += (s, e) =>
            {
                RootFolder = new StorageFolderViewModel(e.RootFolder)
                {
                    IsExpanded = true
                };
            };

            solutionLoader.StorageLibraryChanged += OnSolutionLoaderStorageLibraryChanged;

            EngineAssetViewFactory engineAssetViewFactory = new EngineAssetViewFactory();
            engineAssetViewFactory.Add(typeof(Entity), new SceneEditorViewFactory());

            CodeEditorViewFactory codeEditorViewFactory = new CodeEditorViewFactory();

            editorViewFactory.Add(".xaml", engineAssetViewFactory);
            editorViewFactory.Add(".cs", codeEditorViewFactory);
            editorViewFactory.Add(".vb", codeEditorViewFactory);
            editorViewFactory.Add(".csproj", codeEditorViewFactory);
            editorViewFactory.Add(".vbproj", codeEditorViewFactory);

            AddFileCommand = new RelayCommand<StorageFolderViewModel>(folder => _ = AddFileAsync(folder), folder => folder != null);
            AddFolderCommand = new RelayCommand<StorageFolderViewModel>(folder => _ = AddFolderAsync(folder), folder => folder != null);
            OpenCommand = new RelayCommand<StorageItemViewModel>(item => _ = OpenAsync(item), item => item != null);
            ViewCodeCommand = new RelayCommand<StorageFileViewModel>(file => _ = ViewCodeAsync(file), file => file != null);
            DeleteCommand = new RelayCommand<StorageItemViewModel>(item => _ = DeleteAsync(item), item => item != null);
            ShowPropertiesCommand = new RelayCommand<StorageItemViewModel>(ShowProperties, item => item != null);
            RefreshCommand = new RelayCommand<StorageFolderViewModel>(folder => _ = folder.FillAsync(), folder => folder != null);
        }

        public StorageFolderViewModel? RootFolder
        {
            get => rootFolder;
            set => Set(ref rootFolder, value);
        }

        public RelayCommand<StorageFolderViewModel> AddFileCommand { get; }

        public RelayCommand<StorageFolderViewModel> AddFolderCommand { get; }

        public RelayCommand<StorageItemViewModel> OpenCommand { get; }

        public RelayCommand<StorageFileViewModel> ViewCodeCommand { get; }

        public RelayCommand<StorageItemViewModel> DeleteCommand { get; }

        public RelayCommand<StorageItemViewModel> ShowPropertiesCommand { get; }

        public RelayCommand<StorageFolderViewModel> RefreshCommand { get; }

        public async Task AddFileAsync(StorageFolderViewModel folder)
        {
            await folder.Model.CreateFileAsync("NewFile.cs", CreationCollisionOption.GenerateUniqueName);
        }

        public async Task AddFolderAsync(StorageFolderViewModel folder)
        {
            await folder.Model.CreateFolderAsync("NewFolder", CreationCollisionOption.GenerateUniqueName);
        }

        public async Task OpenAsync(StorageItemViewModel item)
        {
            if (item is StorageFileViewModel file)
            {
                object? editor = await editorViewFactory.CreateAsync(file.Model);

                if (editor != null)
                {
                    tabViewManager.MainTabView.Tabs.Add(editor);
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
                tabViewManager.MainTabView.Tabs.Add(editor);
            }
            else
            {
                await Launcher.LaunchFileAsync(file.Model);
            }
        }

        public async Task DeleteAsync(StorageItemViewModel item)
        {
            RemoveEditors(item.Path);

            await item.Model.DeleteAsync();
        }

        public void RemoveEditors(string path)
        {
            foreach (TabViewViewModel tabView in tabViewManager.TabViews)
            {
                for (int i = tabView.Tabs.Count - 1; i >= 0; i--)
                {
                    if (tabView.Tabs[i] is IFileEditor fileEditor && fileEditor.File.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                    {
                        tabView.Tabs.RemoveAt(i);
                    }
                }
            }
        }

        public void ShowProperties(StorageItemViewModel item)
        {
            tabViewManager.OpenTab(propertyManager, tabViewManager.SolutionExplorerTabView);
            propertyManager.ShowProperties(item.Model);
        }

        private async void OnSolutionLoaderStorageLibraryChanged(object sender, StorageLibraryChangedEventArgs e)
        {
            if (RootFolder is null) return;

            foreach (StorageLibraryChange change in e.Changes)
            {
                if (change.ChangeType == StorageLibraryChangeType.Created || change.ChangeType == StorageLibraryChangeType.MovedIntoLibrary)
                {
                    IStorageItem? item = await change.GetStorageItemAsync();

                    if (item != null)
                    {
                        AddItem(RootFolder, item);
                    }
                }
                else if (change.ChangeType == StorageLibraryChangeType.Deleted || change.ChangeType == StorageLibraryChangeType.MovedOutOfLibrary)
                {
                    RemoveItem(RootFolder, change.Path);
                    RemoveEditors(change.Path);
                }
                else if (change.ChangeType == StorageLibraryChangeType.MovedOrRenamed)
                {
                    RemoveItem(RootFolder, change.PreviousPath);

                    IStorageItem? item = await change.GetStorageItemAsync();

                    if (item != null)
                    {
                        AddItem(RootFolder, item);
                    }
                }
            }
        }

        private static void AddItem(StorageFolderViewModel containingFolder, IStorageItem item)
        {
            if (FindStorageItem(containingFolder, Path.GetDirectoryName(item.Path)) is StorageFolderViewModel folderOfItem)
            {
                StorageItemViewModel? exisitingItem = folderOfItem.Children.FirstOrDefault(i => item.Path.Equals(i.Path, StringComparison.OrdinalIgnoreCase));

                if (exisitingItem is null)
                {
                    if (item is IStorageFile file)
                    {
                        folderOfItem.Children.Add(new StorageFileViewModel(file));
                    }
                    else if (item is IStorageFolder folder)
                    {
                        folderOfItem.Children.Add(new StorageFolderViewModel(folder));
                    }
                }
            }
        }

        private static void RemoveItem(StorageFolderViewModel containingFolder, string path)
        {
            StorageItemViewModel? previousItem = FindStorageItem(containingFolder, path);
            previousItem?.Parent?.Children.Remove(previousItem);
        }

        private static StorageItemViewModel? FindStorageItem(StorageFolderViewModel containingFolder, string path)
        {
            StorageItemViewModel? foundItem = containingFolder;

            while (foundItem != null && !foundItem.Path.Equals(path, StringComparison.OrdinalIgnoreCase) && foundItem is StorageFolderViewModel folder)
            {
                string relativePath = StorageExtensions.GetRelativePath(foundItem.Path, path);
                int indexOfDirectorySeparatorChar = relativePath.IndexOf(Path.DirectorySeparatorChar);
                foundItem = folder.Children.FirstOrDefault(i => relativePath.AsSpan(0, indexOfDirectorySeparatorChar >= 0 ? indexOfDirectorySeparatorChar : relativePath.Length).Equals(i.Name.AsSpan(), StringComparison.OrdinalIgnoreCase));
            }

            return foundItem;
        }
    }
}
