using System.Threading.Tasks;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Editor.ViewModels.Games;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Serialization;
using Microsoft.Toolkit.Mvvm.Commands;
using Microsoft.Toolkit.Mvvm.ObjectModel;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SceneEditorViewModel : ObservableObject
    {
        private bool isLoading;
        private EntityViewModel? rootEntity;

        public SceneEditorViewModel(IStorageFolder rootFolder, string scenePath)
        {
            RootFolder = rootFolder;
            ScenePath = scenePath;

            OpenCommand = new RelayCommand<EntityViewModel>(Open);
            DeleteCommand = new RelayCommand<EntityViewModel>(Delete);

            Game = new EditorGame(new GameContextWithGraphics { FileProvider = new FileSystemProvider(RootFolder) });
            Game.SceneSystem.RootEntity = SceneRootEntity.Model;
        }

        public EditorGame Game { get; }

        public IStorageFolder RootFolder { get; }

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
