using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Games;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class SceneView : UserControl
    {
        public SceneView(StorageFolder rootFolder)
        {
            InitializeComponent();

            EditorGame game = new EditorGame(new GameContextXaml(swapChainPanel), rootFolder);
            game.Run();

            ViewModel = new SceneViewModel(game);
        }

        public SceneViewModel ViewModel { get; }
    }
}
