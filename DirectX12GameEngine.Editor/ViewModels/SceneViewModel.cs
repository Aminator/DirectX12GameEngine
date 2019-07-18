using System.Threading.Tasks;
using DirectX12GameEngine.Engine;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SceneViewModel : ViewModelBase
    {
        private readonly EditorGame game;

        private bool isLoading;

        public SceneViewModel(EditorGame game)
        {
            this.game = game;

            game.SceneSystem.SceneInstance.RootEntity = RootEntity.Model;
        }

        public EntityViewModel RootEntity { get; } = new EntityViewModel(new Entity("RootEntity"));

        public bool IsLoading
        {
            get => isLoading;
            private set => Set(ref isLoading, value);
        }

        public async Task LoadAsync(string path)
        {
            IsLoading = true;
            RootEntity.Children.Clear();

            Entity scene = await game.Content.LoadAsync<Entity>(path);
            EntityViewModel sceneViewModel = new EntityViewModel(scene);

            RootEntity.Children.Add(sceneViewModel);
            IsLoading = false;
        }
    }
}
