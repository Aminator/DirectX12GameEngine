using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Engine
{
    public sealed class Entity : ObservableCollection<EntityComponent>, IIdentifiable
    {
        private Guid id = Guid.NewGuid();
        private string name;
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

            Name = name ?? GetType().Name;

            Transform = transform ?? new TransformComponent();
            Add(Transform);
        }

        [IgnoreDataMember]
        public IList<Entity> Children => Transform.ChildEntities;

        [IgnoreDataMember]
        public ObservableCollection<EntityComponent> Components => this;

        public Guid Id { get => id; set => Set(ref id, value); }

        public string Name { get => name; set => Set(ref name, value); }

        [IgnoreDataMember]
        public Entity? Parent { get => Transform.Parent?.Entity; set => Transform.Parent = value?.Transform; }

        [IgnoreDataMember]
        public EntityManager? EntityManager { get; internal set; }

        [IgnoreDataMember]
        public TransformComponent Transform { get => transform; private set => Set(ref transform, value); }

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

        public override string ToString() => $"Entity {Name}";

        private void AddInternal(EntityComponent entityComponent)
        {
            if (entityComponent.Entity != null)
            {
                throw new InvalidOperationException("An entity component cannot be set on more than one entity.");
            }

            if (entityComponent is TransformComponent transformComponent && Transform != transformComponent)
            {
                Remove(Transform);
                Transform = transformComponent;
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

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                NotifyPropertyChanged(name);
                return true;
            }

            return false;
        }
    }
}
