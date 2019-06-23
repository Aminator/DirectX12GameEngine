using System;
using System.Runtime.Serialization;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(ScriptProcessor))]
    public abstract class ScriptComponent : EntityComponent
    {
#nullable disable
        [IgnoreDataMember]
        public IServiceProvider Services { get; private set; }

        [IgnoreDataMember]
        public ContentManager Content { get; private set; }

        [IgnoreDataMember]
        public GameBase Game { get; private set; }

        [IgnoreDataMember]
        public GraphicsDevice GraphicsDevice { get; private set; }

        [IgnoreDataMember]
        public SceneSystem SceneSystem { get; private set; }

        [IgnoreDataMember]
        public ScriptSystem Script { get; private set; }
#nullable enable

        internal void Initialize(IServiceProvider services)
        {
            Services = services;

            Content = Services.GetRequiredService<ContentManager>();
            Game = Services.GetRequiredService<GameBase>();
            GraphicsDevice = Services.GetRequiredService<GraphicsDevice>();
            SceneSystem = Services.GetRequiredService<SceneSystem>();
            Script = Services.GetRequiredService<ScriptSystem>();
        }

        public virtual void Cancel()
        {
        }
    }
}
