using System.Numerics;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class SceneEditorView : UserControl
    {
        public SceneEditorView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };

            SharedShadow.Receivers.Add(SwapChainPanel);

            EntityTreeView.Translation += new Vector3(0.0f, 0.0f, 32.0f);

            ((StandardUICommand)Resources["OpenCommand"]).KeyboardAccelerators.Clear();
        }

        public SceneEditorViewModel ViewModel => (SceneEditorViewModel)DataContext;
    }
}
