using System;
using System.Numerics;
using System.Threading.Tasks;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Windows.Storage;

#nullable enable

namespace DirectX12GameEngine.Editor
{
    public class EditorGame : Game
    {
        public EditorGame(GameContext context, StorageFolder rootFolder) : base(context)
        {
            Content.RootFolder = rootFolder;
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.Presenter.PresentationParameters.SyncInterval = 1;
            }
        }

        protected override void BeginDraw()
        {
            base.BeginDraw();

            if (GraphicsDevice?.Presenter != null)
            {
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                GraphicsDevice.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, ClearFlags.FlagsDepth);
            }
        }

        protected override async Task LoadContentAsync()
        {
            StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;

            ShaderContent.RootFolder = await temporaryFolder.CreateFolderAsync("ShaderCache", CreationCollisionOption.OpenIfExists);

            await base.LoadContentAsync();
        }
    }
}
