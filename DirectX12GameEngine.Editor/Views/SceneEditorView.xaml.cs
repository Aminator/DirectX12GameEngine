using System;
using System.Numerics;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Input;
using Windows.UI.Xaml;
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

            SharedShadow.Receivers.Add(SwapChainPanel);

            EntityTreeView.Translation += new Vector3(0.0f, 0.0f, 32.0f);

            ((StandardUICommand)Resources["OpenCommand"]).KeyboardAccelerators.Clear();

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Game.Exit();

            ViewModel.Game.Window = new XamlGameWindow(SwapChainPanel);

            if (ViewModel.Game.GraphicsDevice != null)
            {
                PresentationParameters presentationParameters = new PresentationParameters
                {
                    BackBufferWidth = Math.Max(1, (int)(SwapChainPanel.ActualWidth * SwapChainPanel.CompositionScaleX + 0.5f)),
                    BackBufferHeight = Math.Max(1, (int)(SwapChainPanel.ActualHeight * SwapChainPanel.CompositionScaleY + 0.5f))
                };

                ViewModel.Game.GraphicsDevice.Presenter = new XamlSwapChainGraphicsPresenter(ViewModel.Game.GraphicsDevice, presentationParameters, SwapChainPanel);
            }

            ViewModel.Game.Input.Sources.Clear();
            ViewModel.Game.Input.AddSourcesFromConfiguration(new XamlInputSourceConfiguration(SwapChainPanel));

            ViewModel.Game.Run();

            await ViewModel.LoadAsync();
        }

        public SceneEditorViewModel ViewModel => (SceneEditorViewModel)DataContext;
    }
}
