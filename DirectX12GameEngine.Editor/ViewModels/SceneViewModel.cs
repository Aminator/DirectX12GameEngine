using System.Threading.Tasks;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Mvvm.Messaging;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SceneViewModel : ViewModelBase
    {
        private readonly EditorGame game;
        private readonly EntityViewModel sceneRootEntity = new EntityViewModel(new Entity("SceneRootEntity"));

        private bool isLoading;
        private EntityViewModel? rootEntity;

        public SceneViewModel(EditorGame game)
        {
            this.game = game;

            game.SceneSystem.SceneInstance.RootEntity = sceneRootEntity.Model;

            OpenCommand = new RelayCommand<EntityViewModel>(Open);
            DeleteCommand = new RelayCommand<EntityViewModel>(Delete);
        }

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
                        sceneRootEntity.Children.Remove(previousRootEntity);
                    }

                    if (rootEntity != null)
                    {
                        sceneRootEntity.Children.Add(rootEntity);
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
            Messenger.Default.Send(new ShowEntityPropertiesMessage(entity));
        }

        private void Delete(EntityViewModel entity)
        {
            entity.Parent?.Children.Remove(entity);
        }

        public async Task LoadAsync(string path)
        {
            IsLoading = true;
            RootEntity = null;

            Entity scene = await game.Content.LoadAsync<Entity>(path);
            EntityViewModel sceneViewModel = new EntityViewModel(scene);

            RootEntity = sceneViewModel;
            IsLoading = false;
        }
    }
}
