using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Games;

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
                Entity rootEntity = await Content.LoadAsync<Entity>(InitialScenePath);
                SceneInstance = new SceneInstance(Services, rootEntity);
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
