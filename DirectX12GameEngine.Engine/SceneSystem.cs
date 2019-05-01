using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Games;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public sealed class SceneSystem : GameSystem
    {
        public SceneSystem(IServiceProvider services) : base(services)
        {
        }

        public CameraComponent? CurrentCamera { get; set; }

        public string? InitialScenePath { get; set; }

        public SceneInstance? SceneInstance { get; set; }

        public override async Task LoadContentAsync()
        {
            if (InitialScenePath != null)
            {
                Scene rootScene = await Content.LoadAsync<Scene>(InitialScenePath);
                SceneInstance = ActivatorUtilities.CreateInstance<SceneInstance>(Services, rootScene);
            }
        }

        public override void Update(GameTime gameTime)
        {
            SceneInstance?.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            SceneInstance?.Draw(gameTime);
        }

        public override void Dispose()
        {
            SceneInstance?.Dispose();
        }
    }
}
