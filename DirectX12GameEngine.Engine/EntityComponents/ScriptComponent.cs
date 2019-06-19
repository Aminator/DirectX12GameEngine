using System;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public abstract class ScriptComponent : EntityComponent
    {
#nullable disable
        public IServiceProvider Services { get; private set; }

        public ContentManager Content { get; private set; }

        public Game Game { get; private set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        public SceneSystem SceneSystem { get; private set; }
#nullable enable

        internal void Initialize(IServiceProvider services)
        {
            Services = services;

            Content = Services.GetRequiredService<ContentManager>();
            Game = Services.GetRequiredService<Game>();
            GraphicsDevice = Services.GetRequiredService<GraphicsDevice>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();
        }

        public virtual void Cancel()
        {
        }
    }
}
