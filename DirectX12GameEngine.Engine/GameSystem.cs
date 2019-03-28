using System;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public abstract class GameSystem : GameSystemBase
    {
        public GameSystem(IServiceProvider services) : base(services)
        {
            Content = services.GetRequiredService<ContentManager>();
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
        }

        protected ContentManager Content { get; }

        protected GraphicsDevice GraphicsDevice { get; }
    }
}
