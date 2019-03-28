using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public abstract class EntitySystem : IDisposable
    {
        public EntitySystem(Type mainType, IServiceProvider services, params Type[] requiredAdditionalTypes)
        {
            MainType = mainType;
            RequiredTypes = requiredAdditionalTypes;
            Services = services;

            Game = services.GetRequiredService<GameBase>();
            Content = services.GetRequiredService<ContentManager>();
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
            SceneSystem = services.GetRequiredService<SceneSystem>();
        }

        public GameBase Game { get; }

        public Type MainType { get; }

        public Type[] RequiredTypes { get; }

        public IServiceProvider Services { get; }

        public int Order { get; protected set; }

        protected ContentManager Content { get; }

        protected GraphicsDevice GraphicsDevice { get; }

        protected SceneSystem SceneSystem { get; }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(GameTime gameTime)
        {
        }

        public virtual void Dispose()
        {
        }

        protected internal abstract void ProcessEntityComponent(EntityComponent entityComponent, bool forceRemove);

        protected internal void InternalAddEntity(Entity entity)
        {
            SceneSystem.AddInternal(entity);
        }

        protected internal void InternalRemoveEntity(Entity entity, bool removeParent)
        {
            SceneSystem.RemoveInternal(entity, removeParent);
        }

        internal bool Accept(Type type)
        {
            return MainType.IsAssignableFrom(type);
        }
    }

    public abstract class EntitySystem<TComponent> : EntitySystem where TComponent : EntityComponent
    {
        public EntitySystem(IServiceProvider services, params Type[] requiredAdditionalTypes)
            : base(typeof(TComponent), services, requiredAdditionalTypes)
        {
        }

        protected ObservableCollection<TComponent> Components { get; } = new ObservableCollection<TComponent>();

        protected internal override void ProcessEntityComponent(EntityComponent entityComponent, bool forceRemove)
        {
            if (entityComponent.Entity is null) throw new ArgumentException("The entity component must be attached to an entity.", nameof(entityComponent));
            if (!(entityComponent is TComponent component)) throw new ArgumentException("The entity component must be assignable to TComponent", nameof(entityComponent));

            bool entityMatch = !forceRemove && EntityMatch(entityComponent.Entity);
            bool entityAdded = Components.Contains(component);

            if (entityMatch && !entityAdded)
            {
                Components.Add(component);
            }
            else if (!entityMatch && entityAdded)
            {
                Components.Remove(component);
            }
        }

        private bool EntityMatch(Entity entity)
        {
            if (RequiredTypes.Length == 0) return true;

            List<Type> remainingRequiredTypes = new List<Type>(RequiredTypes);

            foreach (EntityComponent component in entity.Components)
            {
                remainingRequiredTypes.RemoveAll(t => t.IsAssignableFrom(component.GetType()));

                if (remainingRequiredTypes.Count == 0) return true;
            }

            return false;
        }
    }
}
