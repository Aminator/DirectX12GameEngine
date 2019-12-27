using System.Threading.Tasks;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Mvvm.Messaging;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Games;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SceneEditorViewModel : ViewModelBase
    {
        private bool isLoading;
        private EntityViewModel? rootEntity;

        public SceneEditorViewModel(StorageFolderViewModel rootFolder, string scenePath)
        {
            RootFolder = rootFolder;
            ScenePath = scenePath;

            OpenCommand = new RelayCommand<EntityViewModel>(Open);
            DeleteCommand = new RelayCommand<EntityViewModel>(Delete);

            Game = new EditorGame(new GameContextWithGraphics { FileProvider = new FileSystemProvider(RootFolder.Model) });
            Game.Run();

            Game.SceneSystem.SceneInstance.RootEntity = SceneRootEntity.Model;

            _ = LoadAsync();
        }

        public EditorGame Game { get; }

        public StorageFolderViewModel RootFolder { get; }

        public string ScenePath { get; }

        public EntityViewModel SceneRootEntity { get; } = new EntityViewModel(new Entity("SceneRootEntity"));

        public EntityViewModel? RootEntity
        {
            get => rootEntity;
            set
            {
                EntityViewModel? previousRootEntity = rootEntity;

                if (Set(ref rootEntity, value))
                {
                    if (previousRootEntity != null)
                    {
                        SceneRootEntity.Children.Remove(previousRootEntity);
                    }

                    if (rootEntity != null)
                    {
                        SceneRootEntity.Children.Add(rootEntity);
                    }
                }
            }
        }

        public bool IsLoading
        {
            get => isLoading;
            private set => Set(ref isLoading, value);
        }

        public RelayCommand<EntityViewModel> OpenCommand { get; }

        public RelayCommand<EntityViewModel> DeleteCommand { get; }

        private void Open(EntityViewModel entity)
        {
            Messenger.Default.Send(new ShowPropertiesMessage(entity.Model));
        }

        private void Delete(EntityViewModel entity)
        {
            entity.Parent?.Children.Remove(entity);
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            RootEntity = null;

            Entity scene = await Game.Content.LoadAsync<Entity>(ScenePath);
            EntityViewModel sceneViewModel = new EntityViewModel(scene);

            RootEntity = sceneViewModel;
            IsLoading = false;
        }
    }
}
