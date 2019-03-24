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
            Game = services.GetRequiredService<Game>();
            Content = services.GetRequiredService<ContentManager>();
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
        }

        public Game Game { get; }

        protected ContentManager Content { get; }

        protected GraphicsDevice GraphicsDevice { get; }
    }
}
