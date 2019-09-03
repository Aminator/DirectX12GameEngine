using System.Numerics;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Games;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class SceneView : UserControl
    {
        public SceneView(StorageFolderViewModel rootFolder)
        {
            InitializeComponent();

            SharedShadow.Receivers.Add(SwapChainPanel);

            EntityTreeView.Translation += new Vector3(0.0f, 0.0f, 32.0f);

            ((StandardUICommand)Resources["OpenCommand"]).KeyboardAccelerators.Clear();

            EditorGame game = new EditorGame(new XamlGameContext(SwapChainPanel), rootFolder.Model);
            //EditorGame game = new EditorGame(new GameContextWithGraphics(), rootFolder.Model);
            game.Run();

            ViewModel = new SceneViewModel(game);
        }

        public SceneViewModel ViewModel { get; }
    }
}
