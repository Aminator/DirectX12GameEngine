using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class SceneView : UserControl
    {
        private readonly EditorGame game;

        public SceneView(StorageFolder rootFolder)
        {
            InitializeComponent();

            game = new EditorGame(new GameContextXaml(swapChainPanel), rootFolder);
            game.Run();

            game.SceneSystem.SceneInstance.RootEntity = ViewModel.Model;
        }

        public EntityViewModel ViewModel { get; } = new EntityViewModel(new Entity("RootEntity"));

        public async Task LoadAsync(string path)
        {
            ViewModel.Children.Clear();

            Entity scene = await game.Content.LoadAsync<Entity>(path);

            EntityViewModel sceneViewModel = new EntityViewModel(scene);
            ViewModel.Children.Add(sceneViewModel);
        }
    }
}
