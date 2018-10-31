using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DirectX12GameEngine
{
    public sealed class SceneSystem : GameSystem
    {
        private readonly HashSet<Type> componentTypes = new HashSet<Type>();
        private readonly HashSet<Entity> entities = new HashSet<Entity>();

        private Scene? rootScene;

        public SceneSystem(IServiceProvider services) : base(services)
        {
        }

        public HashSet<EntitySystem> EntitySystems { get; } = new HashSet<EntitySystem>();

        public CameraComponent? CurrentCamera { get; set; }

        public string? InitialScenePath { get; set; }

        public Scene? RootScene
        {
            get => rootScene;
            set
            {
                if (rootScene == value) return;

                if (rootScene != null)
                {
                    Remove(rootScene);
                }

                if (value != null)
                {
                    Add(value);
                }

                rootScene = value;
            }
        }

        public override async Task LoadContentAsync()
        {
            if (InitialScenePath != null)
            {
                RootScene = await Content.LoadAsync<Scene>(InitialScenePath);
            }
        }

        public override void Update(TimeSpan deltaTime)
        {
            lock (EntitySystems)
            {
                foreach (EntitySystem entitySystem in EntitySystems)
                {
                    entitySystem.Update(deltaTime);
                }
            }
        }

        public override void Draw(TimeSpan deltaTime)
        {
            lock (EntitySystems)
            {
                foreach (EntitySystem entitySystem in EntitySystems)
                {
                    entitySystem.Draw(deltaTime);
                }
            }
        }

        public override void Dispose()
        {
            foreach (EntitySystem entitySystem in EntitySystems)
            {
                entitySystem.Dispose();
            }
        }

        private void Add(Scene scene)
        {
            foreach (Entity entity in scene)
            {
                Add(entity);
            }

            scene.CollectionChanged += Entities_CollectionChanged;
        }

        private void Add(Entity entity)
        {
            if (!entities.Add(entity)) return;

            foreach (EntityComponent entityComponent in entity)
            {
                Add(entityComponent);
            }

            entity.CollectionChanged += Components_CollectionChanged;
        }

        private void Add(EntityComponent entityComponent)
        {
            lock (EntitySystems)
            {
                Type componentType = entityComponent.GetType();
                CollectNewEntitySystems(componentType);

                var entitySystems = EntitySystems.Where(e => e.MainType == componentType);

                foreach (EntitySystem entitySystem in entitySystems)
                {
                    entitySystem.ProcessEntityComponent(entityComponent, false);
                }
            }
        }

        private void CollectNewEntitySystems(Type componentType)
        {
            if (!componentTypes.Add(componentType)) return;

            IEnumerable<DefaultEntitySystemAttribute> entitySystemAttributes =
                componentType.GetCustomAttributes<DefaultEntitySystemAttribute>();

            foreach (DefaultEntitySystemAttribute entitySystemAttribute in entitySystemAttributes)
            {
                bool addNewSystem = true;

                foreach (EntitySystem system in EntitySystems)
                {
                    if (system.GetType() == entitySystemAttribute.Type)
                    {
                        addNewSystem = false;
                        break;
                    }
                }

                if (addNewSystem)
                {
                    EntitySystem entitySystem = (EntitySystem)Activator.CreateInstance(entitySystemAttribute.Type, Services);
                    EntitySystems.Add(entitySystem);

                    foreach (Type additionalType in entitySystem.RequiredTypes)
                    {
                        CollectNewEntitySystems(additionalType);
                    }
                }
            }
        }

        private void Remove(Scene scene)
        {
            scene.CollectionChanged -= Entities_CollectionChanged;

            foreach (Entity entity in scene)
            {
                Remove(entity);
            }
        }

        private void Remove(Entity entity)
        {
            if (!entities.Remove(entity)) return;

            entity.CollectionChanged -= Components_CollectionChanged;

            foreach (EntityComponent entityComponent in entity)
            {
                Remove(entityComponent);
            }
        }

        private void Remove(EntityComponent entityComponent)
        {
            lock (EntitySystems)
            {
                Type componentType = entityComponent.GetType();

                var entitySystems = EntitySystems.Where(e => e.MainType == componentType);

                foreach (EntitySystem entitySystem in entitySystems)
                {
                    entitySystem.ProcessEntityComponent(entityComponent, true);
                }
            }
        }

        private void Entities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Entity entity in e.NewItems)
                    {
                        Add(entity);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Entity entity in e.OldItems)
                    {
                        Remove(entity);
                    }
                    break;
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
