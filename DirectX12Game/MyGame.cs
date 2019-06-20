using System;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Assets;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

namespace DirectX12Game
{
    public sealed class MyGame : Game
    {
        public MyGame(GameContext gameContext) : base(gameContext)
        {
            if (GraphicsDevice.Presenter != null)
            {
                GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
            }

            SceneSystem.InitialScenePath = @"Assets\Scenes\Scene1.xml";
        }

        protected override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice.Presenter != null)
            {
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.25f, 0.5f, 1.0f));
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, SharpDX.Direct3D12.ClearFlags.FlagsDepth);
            }
        }

        protected override async Task LoadContentAsync()
        {
            Content.RootFolder = await StorageFolder.GetFolderFromPathAsync(Directory.GetCurrentDirectory());

            ShaderContentManager shaderContentManager = Services.GetRequiredService<ShaderContentManager>();

            StorageFolder temporaryFolder;

            try
            {
                temporaryFolder = ApplicationData.Current.TemporaryFolder;
            }
            catch
            {
                temporaryFolder = await Content.RootFolder.CreateFolderAsync("TempState", CreationCollisionOption.OpenIfExists);
            }

            shaderContentManager.RootFolder = await temporaryFolder.CreateFolderAsync("ShaderCache", CreationCollisionOption.OpenIfExists);

            // TODO: DirectX12GameEngine.Assets.dll does not get copied to the output directory if it is never used.
            MaterialAsset materialAsset = new MaterialAsset(Content, shaderContentManager, GraphicsDevice);
            materialAsset.ToString();

            await base.LoadContentAsync();
        }
    }
}
