using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public abstract class GameSystem : IDisposable
    {
        public GameSystem(IServiceProvider services)
        {
            Services = services;

            Game = services.GetRequiredService<Game>();
            Content = services.GetRequiredService<ContentManager>();
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
        }

        public Game Game { get; }

        public IServiceProvider Services { get; }

        protected ContentManager Content { get; }

        protected GraphicsDevice GraphicsDevice { get; }

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

        public virtual void Update(TimeSpan deltaTime)
        {
        }

        public virtual void BeginDraw()
        {
        }

        public virtual void Draw(TimeSpan deltaTime)
        {
        }

        public virtual void EndDraw()
        {
        }
    }
}
