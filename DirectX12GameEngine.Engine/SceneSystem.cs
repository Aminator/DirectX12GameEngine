using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Games;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public sealed class SceneSystem : GameSystemBase
    {
        private readonly ContentManager content;

        public SceneSystem(IServiceProvider services)
        {
            SceneInstance = new SceneInstance(services);
            content = services.GetRequiredService<ContentManager>();
        }

        public CameraComponent? CurrentCamera { get; set; }

        public string? InitialScenePath { get; set; }

        public SceneInstance SceneInstance { get; set; }

        public override async Task LoadContentAsync()
        {
            if (InitialScenePath != null)
            {
                SceneInstance.RootEntity = await content.LoadAsync<Entity>(InitialScenePath);
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
