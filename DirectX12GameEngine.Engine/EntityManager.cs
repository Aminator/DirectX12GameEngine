using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using DirectX12GameEngine.Games;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public abstract class EntityManager : IEnumerable<Entity>, IDisposable
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
            lock (Systems)
            {
                foreach (EntitySystem system in Systems)
                {
                    system.Update(gameTime);
                }
            }
        }

        public virtual void Draw(GameTime gameTime)
        {
            lock (Systems)
            {
                foreach (EntitySystem system in Systems)
                {
                    system.Draw(gameTime);
                }
            }
        }

        public virtual void Dispose()
        {
            foreach (EntitySystem system in Systems)
            {
                system.Dispose();
            }
        }

        internal void Add(Entity entity)
        {
            if (entity.Transform.Parent != null)
            {
                throw new ArgumentException("This entity should not have a parent.", nameof(entity));
            }

            AddInternal(entity);
        }

        internal void AddInternal(Entity entity)
        {
            if (!entities.Add(entity)) return;

            foreach (EntityComponent entityComponent in entity)
            {
                Add(entityComponent);
            }

            entity.CollectionChanged += Components_CollectionChanged;
        }

        internal void Remove(Entity entity)
        {
            RemoveInternal(entity, true);
        }

        internal void RemoveInternal(Entity entity, bool removeParent)
        {
            if (!entities.Remove(entity)) return;

            entity.CollectionChanged -= Components_CollectionChanged;

            if (removeParent)
            {
                entity.Transform.Parent = null;
            }

            foreach (EntityComponent entityComponent in entity)
            {
                Remove(entityComponent);
            }
        }

        private void Add(EntityComponent entityComponent)
        {
            CheckEntityComponentWithSystems(entityComponent, false);
        }

        private void Remove(EntityComponent entityComponent)
        {
            CheckEntityComponentWithSystems(entityComponent, true);
        }

        private void CheckEntityComponentWithSystems(EntityComponent entityComponent, bool forceRemove)
        {
            if (entityComponent.Entity is null) throw new ArgumentException("The entity component must be attached to an entity.", nameof(entityComponent));

            lock (Systems)
            {
                Type componentType = entityComponent.GetType();

                if (systemsPerComponentType.TryGetValue(componentType, out var systemsForComponent))
                {
                    foreach (EntitySystem system in systemsForComponent)
                    {
                        system.ProcessEntityComponent(entityComponent, forceRemove);
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
                        if (system.Accept(componentType))
                        {
                            systemsForComponent.Add(system);
                            system.ProcessEntityComponent(entityComponent, forceRemove);
                        }
                    }

                    systemsPerComponentType.Add(componentType, systemsForComponent);
                }

                UpdateDependentSystems(entityComponent.Entity, entityComponent);
            }
        }

        private void CollectNewEntitySystems(Type componentType)
        {
            var entitySystemAttributes = componentType.GetCustomAttributes<DefaultEntitySystemAttribute>();

            foreach (DefaultEntitySystemAttribute entitySystemAttribute in entitySystemAttributes)
            {
                bool addNewSystem = !Systems.Exists(s => s.GetType() == entitySystemAttribute.Type);

                if (addNewSystem)
                {
                    EntitySystem system = (EntitySystem)ActivatorUtilities.CreateInstance(Services, entitySystemAttribute.Type);
                    system.EntityManager = this;

                    Systems.Add(system);
                    Systems.Sort(EntitySystemCollection.EntitySystemComparer.Default);
                }
            }
        }

        private void UpdateDependentSystems(Entity entity, EntityComponent skipComponent)
        {
            foreach (EntityComponent entityComponent in entity.Components)
            {
                if (entityComponent == skipComponent) continue;

                Type componentType = entityComponent.GetType();

                if (systemsPerComponentType.TryGetValue(componentType, out var systemsForComponent))
                {
                    foreach (EntitySystem system in systemsForComponent)
                    {
                        system.ProcessEntityComponent(entityComponent, false);
                    }
                }
            }
        }

        private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (EntityComponent entityComponent in e.NewItems)
                    {
                        Add(entityComponent);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (EntityComponent entityComponent in e.OldItems)
                    {
                        Remove(entityComponent);
                    }
                    break;
            }
        }
    }
}
