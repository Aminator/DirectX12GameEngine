using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            SceneSystem = services.GetRequiredService<SceneSystem>();
        }

        public Game Game { get; }

        public Type MainType { get; }

        public Type[] RequiredTypes { get; }

        public IServiceProvider Services { get; }

        public int Order { get; protected set; }

        protected ContentManager Content { get; }

        protected GraphicsDevice GraphicsDevice { get; }

        protected SceneSystem SceneSystem { get; }

        public virtual void Update(TimeSpan deltaTime)
        {
        }

        public virtual void Draw(TimeSpan deltaTime)
        {
        }

        public virtual void Dispose()
        {
        }

        internal bool Accept(Type type)
        {
            return MainType.IsAssignableFrom(type);
        }

        protected internal abstract void ProcessEntityComponent(EntityComponent entityComponent, bool forceRemove);
    }

    public abstract class EntitySystem<TComponent> : EntitySystem where TComponent : EntityComponent
    {
        public EntitySystem(IServiceProvider services, params Type[] requiredAdditionalTypes)
            : base(typeof(TComponent), services, requiredAdditionalTypes)
        {
        }

        public ObservableCollection<TComponent> Components { get; } = new ObservableCollection<TComponent>();

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
