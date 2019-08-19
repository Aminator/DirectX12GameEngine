using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using DirectX12GameEngine.Assets;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Windows.Storage;

namespace DirectX12Game
{
    public sealed class MyGame : Game
    {
        public MyGame(GameContext context) : base(context)
        {
            SceneSystem.InitialScenePath = @"Assets\Scenes\Scene1";
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 0;
            }
        }

        protected override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new Vector4(0.0f, 0.25f, 0.5f, 1.0f));
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, ClearFlags.FlagsDepth);
            }
        }

        protected override async Task LoadContentAsync()
        {
            Content.RootFolder = await StorageFolder.GetFolderFromPathAsync(Directory.GetCurrentDirectory());

            StorageFolder temporaryFolder;

            try
            {
                temporaryFolder = ApplicationData.Current.TemporaryFolder;
            }
            catch
            {
                temporaryFolder = await Content.RootFolder.CreateFolderAsync("TempState", CreationCollisionOption.OpenIfExists);
            }

            ShaderContent.RootFolder = await temporaryFolder.CreateFolderAsync("ShaderCache", CreationCollisionOption.OpenIfExists);

            // TODO: DirectX12GameEngine.Assets.dll does not get copied to the output directory if it is never used.
            MaterialAsset materialAsset = new MaterialAsset();
            materialAsset.ToString();

            await base.LoadContentAsync();
        }
    }
}
