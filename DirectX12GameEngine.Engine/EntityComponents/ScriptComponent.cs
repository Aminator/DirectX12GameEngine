using System;
using DirectX12GameEngine.Serialization;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Input;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(EntityScriptSystem))]
    public abstract class ScriptComponent : EntityComponent
    {
        public GraphicsDevice? GraphicsDevice { get; private set; }

#nullable disable
        public IServiceProvider Services { get; private set; }

        public IContentManager Content { get; private set; }

        public IGame Game { get; private set; }

        public InputManager Input { get; private set; }

        public SceneSystem SceneSystem { get; private set; }

        public ScriptSystem Script { get; private set; }
#nullable enable

        internal void Initialize(IServiceProvider services)
        {
            Services = services;

            GraphicsDevice = Services.GetService<GraphicsDevice>();

            Content = Services.GetRequiredService<IContentManager>();
            Game = Services.GetRequiredService<IGame>();
            Input = Services.GetRequiredService<InputManager>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();
            Script = Services.GetRequiredService<ScriptSystem>();
        }

        public virtual void Cancel()
        {
        }
    }
}
