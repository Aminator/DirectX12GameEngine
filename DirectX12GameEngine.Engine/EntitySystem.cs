using System;
using System.Collections.Generic;
using DirectX12GameEngine.Games;

namespace DirectX12GameEngine.Engine
{
    public abstract class EntitySystem : IDisposable
    {
        public EntitySystem(Type mainType, IServiceProvider services, params Type[] requiredAdditionalTypes)
        {
            MainType = mainType;
            RequiredTypes = requiredAdditionalTypes;
            Services = services;
        }

        public EntityManager? EntityManager { get; internal set; }

        public Type MainType { get; }

        public Type[] RequiredTypes { get; }

        public IServiceProvider Services { get; }

        public int Order { get; protected set; }

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
            EntityManager?.AddInternal(entity);
        }

        protected internal void InternalRemoveEntity(Entity entity, bool removeParent)
        {
            EntityManager?.RemoveInternal(entity, removeParent);
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

        protected HashSet<TComponent> Components { get; } = new HashSet<TComponent>();

        protected internal override void ProcessEntityComponent(EntityComponent entityComponent, bool forceRemove)
        {
            if (entityComponent.Entity is null) throw new ArgumentException("The entity component must be attached to an entity.", nameof(entityComponent));
            if (!(entityComponent is TComponent component)) throw new ArgumentException("The entity component must be assignable to TComponent", nameof(entityComponent));

            bool entityMatch = !forceRemove && EntityMatch(entityComponent.Entity);
            bool entityAdded = Components.Contains(component);

            if (entityMatch && !entityAdded)
            {
                OnEntityComponentAdded(component);
                Components.Add(component);
            }
            else if (!entityMatch && entityAdded)
            {
                OnEntityComponentRemoved(component);
                Components.Remove(component);
            }
        }

        protected virtual void OnEntityComponentAdded(TComponent component)
        {
        }

        protected virtual void OnEntityComponentRemoved(TComponent component)
        {
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
