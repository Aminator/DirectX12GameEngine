using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine
{
    public abstract class EntitySystem : IDisposable
    {
        public EntitySystem(Type mainType, IServiceProvider services, params Type[] requiredAdditionalTypes)
        {
            MainType = mainType;
            RequiredTypes = requiredAdditionalTypes;
            Services = services;

            Game = services.GetRequiredService<Game>();
            Content = services.GetRequiredService<ContentManager>();
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
        }

        public Game Game { get; }

        public Type MainType { get; }

        public Type[] RequiredTypes { get; }

        public IServiceProvider Services { get; }

        public int Order { get; protected set; }

        protected ContentManager Content { get; }

        protected GraphicsDevice GraphicsDevice { get; }

        public virtual void Update(TimeSpan deltaTime)
        {
        }

        public virtual void Draw(TimeSpan deltaTime)
        {
        }

        public virtual void Dispose()
        {
        }

        protected internal abstract void ProcessEntityComponent(EntityComponent entityComponent, bool remove);
    }

    public abstract class EntitySystem<TComponent> : EntitySystem where TComponent : EntityComponent
    {
        public EntitySystem(IServiceProvider services, params Type[] requiredAdditionalTypes)
            : base(typeof(TComponent), services, requiredAdditionalTypes)
        {
        }

        public ObservableCollection<TComponent> Components { get; } = new ObservableCollection<TComponent>();

        protected internal override void ProcessEntityComponent(EntityComponent entityComponent, bool remove)
        {
            if (entityComponent.GetType() == typeof(TComponent))
            {
                Entity entity = entityComponent.Entity ?? throw new ArgumentException("The entity component  must be attached to an entity.");

                foreach (Type type in RequiredTypes)
                {
                    if (entity.Where(c => c.GetType() == type).Count() == 0)
                    {
                        return;
                    }
                }

                if (remove)
                {
                    Components.Remove((TComponent)entityComponent);
                }
                else
                {
                    Components.Add((TComponent)entityComponent);
                }
            }
        }
    }
}
