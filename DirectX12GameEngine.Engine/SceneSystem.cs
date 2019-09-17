using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Games;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public sealed class SceneSystem : GameSystemBase
    {
        private readonly IContentManager contentManager;

        public SceneSystem(IServiceProvider services)
        {
            SceneInstance = new SceneInstance(services);
            contentManager = services.GetRequiredService<IContentManager>();
        }

        public CameraComponent? CurrentCamera { get; set; }

        public string? InitialScenePath { get; set; }

        public SceneInstance SceneInstance { get; set; }

        public override async Task LoadContentAsync()
        {
            if (InitialScenePath != null)
            {
                SceneInstance.RootEntity = await contentManager.LoadAsync<Entity>(InitialScenePath);
            }
        }

        public override void Update(GameTime gameTime)
        {
            SceneInstance.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            SceneInstance.Draw(gameTime);
        }

        public override void Dispose()
        {
            SceneInstance.Dispose();
        }
    }
}
