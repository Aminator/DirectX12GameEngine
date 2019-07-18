using System.Threading.Tasks;
using DirectX12GameEngine.Engine;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SceneViewModel : ViewModelBase
    {
        private readonly EditorGame game;

        public SceneViewModel(EditorGame game)
        {
            this.game = game;

            game.SceneSystem.SceneInstance.RootEntity = RootEntity.Model;
        }

        public EntityViewModel RootEntity { get; } = new EntityViewModel(new Entity("RootEntity"));

        public async Task LoadAsync(string path)
        {
            RootEntity.Children.Clear();

            Entity scene = await game.Content.LoadAsync<Entity>(path);
            EntityViewModel sceneViewModel = new EntityViewModel(scene);

            RootEntity.Children.Add(sceneViewModel);
        }
    }
}
