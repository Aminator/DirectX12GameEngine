using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace DirectX12GameEngine.Engine
{
    public sealed class Entity : ObservableCollection<EntityComponent>
    {
        private TransformComponent transform;

        public Entity() : this(null)
        {
        }

        public Entity(string? name) : this(name, null)
        {
        }

        public Entity(string? name, TransformComponent? transform)
        {
            CollectionChanged += Components_CollectionChanged;

            Name = name ?? nameof(Entity) + Id;
            this.transform = transform ?? new TransformComponent();
            Add(this.transform);
        }

        public ObservableCollection<EntityComponent> Components => this;

        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; }

        public Scene? Scene { get; internal set; }

        public TransformComponent Transform
        {
            get => transform;
            set
            {
                if (transform == value) return;

                Remove(transform);
                transform = value;

                if (!Contains(transform))
                {
                    Add(transform);
                }
            }
        }

        public T? Get<T>() where T : EntityComponent
        {
            return this.OfType<T>().FirstOrDefault();
        }

        public T GetOrCreate<T>() where T : EntityComponent, new()
        {
            T? entityComponent = Get<T>();

            if (entityComponent is null)
            {
                entityComponent = new T();
                Add(entityComponent);
            }

            return entityComponent;
        }

        public bool Remove<T>() where T : EntityComponent
        {
            T? entityComponent = Get<T>();

            if (entityComponent != null)
            {
                return Remove(entityComponent);
            }

            return false;
        }

        private void AddInternal(EntityComponent entityComponent)
        {
            if (entityComponent is TransformComponent transformComponent)
            {
                Transform = transformComponent;
            }

            if (entityComponent.Entity != null)
            {
                throw new InvalidOperationException("An entity component cannot be set on more than one entity.");
            }

            entityComponent.Entity = this;
        }

        private void RemoveInternal(EntityComponent entityComponent)
        {
            if (entityComponent.Entity != this)
            {
                throw new InvalidOperationException("This entity component is not set on this entity.");
            }

            entityComponent.Entity = null;
        }

        private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (EntityComponent entityComponent in e.NewItems)
                    {
                        AddInternal(entityComponent);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (EntityComponent entityComponent in e.OldItems)
                    {
                        RemoveInternal(entityComponent);
                    }
                    break;
            }
        }
    }
}
