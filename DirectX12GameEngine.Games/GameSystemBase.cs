using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Games
{
    public class GameSystemBase
    {
        public GameSystemBase(IServiceProvider services)
        {
            Services = services;
            Game = services.GetRequiredService<GameBase>();
        }

        public GameBase Game { get; }

        public IServiceProvider Services { get; }

        public virtual void Dispose()
        {
        }

        public virtual void Initialize()
        {
        }

        public virtual Task LoadContentAsync()
        {
            return Task.CompletedTask;
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void BeginDraw()
        {
        }

        public virtual void Draw(GameTime gameTime)
        {
        }

        public virtual void EndDraw()
        {
        }
    }
}
