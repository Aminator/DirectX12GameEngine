using System;
using System.Numerics;
using System.Threading.Tasks;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Windows.ApplicationModel;
using Windows.Storage;

namespace DirectX12GameEngine.Editor
{
    public class EditorGame : Game
    {
        public EditorGame(GameContext gameContext) : base(gameContext)
        {
            if (GraphicsDevice.Presenter != null)
            {
                GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
            }
        }

        protected override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice.Presenter != null)
            {
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new Vector4(0.0f, 0.25f, 0.5f, 1.0f));
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, ClearFlags.FlagsDepth);
            }
        }

        protected override async Task LoadContentAsync()
        {
            Content.RootFolder = Package.Current.InstalledLocation;

            StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;

            ShaderContent.RootFolder = await temporaryFolder.CreateFolderAsync("ShaderCache", CreationCollisionOption.OpenIfExists);

            await base.LoadContentAsync();
        }
    }
}
