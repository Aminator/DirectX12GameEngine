using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using DirectX12GameEngine.Games;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public abstract class EntityManager : IGameSystem, IEnumerable<Entity>
    {
        private readonly HashSet<Entity> entities = new HashSet<Entity>();
        private readonly Dictionary<Type, List<EntitySystem>> systemsPerComponentType = new Dictionary<Type, List<EntitySystem>>();

        public EntityManager(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public EntitySystemCollection Systems { get; } = new EntitySystemCollection();

        public IEnumerator<Entity> GetEnumerator() => entities.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public virtual void Update(GameTime gameTime)
        {
            foreach (EntitySystem system in Systems)
            {
                system.Update(gameTime);
            }
        }

        public void BeginDraw()
        {
            foreach (EntitySystem system in Systems)
            {
                system.BeginDraw();
            }
        }

        public virtual void Draw(GameTime gameTime)
        {
            foreach (EntitySystem system in Systems)
            {
                system.Draw(gameTime);
            }
        }

        public void EndDraw()
        {
            foreach (EntitySystem system in Systems)
            {
                system.EndDraw();
            }
        }

        public virtual void Dispose()
        {
            foreach (EntitySystem system in Systems)
            {
                system.Dispose();
            }
        }

        internal void AddRoot(Entity entity)
        {
            if (entity.Parent != null)
            {
                throw new ArgumentException("This entity should not have a parent.", nameof(entity));
            }

            Add(entity);
        }

        internal void Add(Entity entity)
        {
            if (entities.Contains(entity)) return;

            if (entity.EntityManager != null)
            {
                throw new ArgumentException("This entity is already used by another entity manager.", nameof(entity));
            }

            entity.EntityManager = this;

            entities.Add(entity);

            foreach (EntityComponent component in entity)
            {
                Add(component, entity);
            }

            foreach (Entity child in entity.Children)
            {
                Add(child);
            }

            entity.Children.CollectionChanged += OnChildrenCollectionChanged;
            entity.Components.CollectionChanged += OnComponentsCollectionChanged;
        }

        internal void RemoveRoot(Entity entity)
        {
            Remove(entity);
        }

        internal void Remove(Entity entity)
        {
            if (!entities.Remove(entity)) return;

            entity.Components.CollectionChanged -= OnComponentsCollectionChanged;
            entity.Children.CollectionChanged -= OnChildrenCollectionChanged;

            foreach (EntityComponent component in entity)
            {
                Remove(component, entity);
            }

            foreach (Entity child in entity.Children)
            {
                Remove(child);
            }

            entity.EntityManager = null;
        }

        private void Add(EntityComponent component, Entity entity)
        {
            CheckEntityComponentWithSystems(component, entity, false);
        }

        private void Remove(EntityComponent component, Entity entity)
        {
            CheckEntityComponentWithSystems(component, entity, true);
        }

        private void CheckEntityComponentWithSystems(EntityComponent component, Entity entity, bool forceRemove)
        {
            Type componentType = component.GetType();

            if (systemsPerComponentType.TryGetValue(componentType, out var systemsForComponent))
            {
                foreach (EntitySystem system in systemsForComponent)
                {
                    system.ProcessEntityComponent(component, entity, forceRemove);
                }
            }
            else
            {
                if (!forceRemove)
                {
                    CollectNewEntitySystems(componentType);
                }

                systemsForComponent = new List<EntitySystem>();

                foreach (EntitySystem system in Systems)
                {
                    if (system.Accepts(componentType))
                    {
                        systemsForComponent.Add(system);
                    }
                }

                systemsPerComponentType.Add(componentType, systemsForComponent);

                foreach (EntitySystem system in systemsForComponent)
                {
                    system.ProcessEntityComponent(component, entity, forceRemove);
                }
            }
        }

        private void CollectNewEntitySystems(Type componentType)
        {
            var entitySystemAttributes = componentType.GetCustomAttributes<DefaultEntitySystemAttribute>();

            foreach (DefaultEntitySystemAttribute entitySystemAttribute in entitySystemAttributes)
            {
                bool addNewSystem = !Systems.Any(s => s.GetType() == entitySystemAttribute.Type);

                if (addNewSystem)
                {
                    EntitySystem system = (EntitySystem)ActivatorUtilities.CreateInstance(Services, entitySystemAttribute.Type);
                    system.EntityManager = this;

                    Systems.Add(system);
                }
            }
        }

        private void UpdateDependentSystems(Entity entity, EntityComponent skipComponent)
        {
            foreach (EntityComponent component in entity.Components)
            {
                if (component == skipComponent) continue;

                Type componentType = component.GetType();

                if (systemsPerComponentType.TryGetValue(componentType, out var systemsForComponent))
                {
                    foreach (EntitySystem system in systemsForComponent)
                    {
                        system.ProcessEntityComponent(component, entity, false);
                    }
                }
            }
        }

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Entity entity in e.NewItems.Cast<Entity>())
                    {
                        Add(entity);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Entity entity in e.OldItems.Cast<Entity>())
                    {
                        Remove(entity);
                    }
                    break;
            }
        }

        private void OnComponentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EntityComponentCollection entityComponentCollection = (EntityComponentCollection)sender;
            Entity entity = entityComponentCollection.Entity;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (EntityComponent component in e.NewItems.Cast<EntityComponent>())
                    {
                        Add(component, entity);
                        UpdateDependentSystems(entity, component);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (EntityComponent component in e.OldItems.Cast<EntityComponent>())
                    {
                        Remove(component, entity);
                        UpdateDependentSystems(entity, component);
                    }
                    break;
            }
        }
    }
}
