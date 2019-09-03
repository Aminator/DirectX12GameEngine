using System;
using System.Collections;
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
    public sealed class Entity : IEnumerable<EntityComponent>, IIdentifiable, INotifyPropertyChanged
    {
        private Entity? parent;

        private Guid id = Guid.NewGuid();
        private string name;
        private TransformComponent transform = new TransformComponent();

        public Entity() : this("Entity")
        {
        }

        public Entity(string name)
        {
            this.name = name;

            Children.CollectionChanged += Children_CollectionChanged;
            Components.CollectionChanged += Components_CollectionChanged;

            Components.Add(Transform);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Guid Id { get => id; set => Set(ref id, value); }

        public string Name { get => name; set => Set(ref name, value); }

        public ObservableCollection<Entity> Children { get; } = new ObservableCollection<Entity>();

        public ObservableCollection<EntityComponent> Components { get; } = new ObservableCollection<EntityComponent>();

        [IgnoreDataMember]
        public Entity? Parent
        {
            get => parent;
            set
            {
                Entity? oldParent = parent;

                if (oldParent == value) return;

                oldParent?.Children.Remove(this);
                value?.Children.Add(this);
            }
        }

        [IgnoreDataMember]
        public EntityManager? EntityManager { get; internal set; }

        [IgnoreDataMember]
        public TransformComponent Transform { get => transform; private set => Set(ref transform, value); }

        public IEnumerator<EntityComponent> GetEnumerator() => Components.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(EntityComponent component)
        {
            Components.Add(component);
        }

        public T Get<T>() where T : EntityComponent
        {
            return this.OfType<T>().FirstOrDefault();
        }

        public T GetOrCreate<T>() where T : EntityComponent, new()
        {
            T component = Get<T>();

            if (component is null)
            {
                component = new T();
                Components.Add(component);
            }

            return component;
        }

        public bool Remove<T>() where T : EntityComponent
        {
            T component = Get<T>();

            if (component != null)
            {
                return Components.Remove(component);
            }

            return false;
        }

        public override string ToString() => $"Entity {Name}";

        private void AddInternal(Entity entity)
        {
            if (entity.Parent != null)
            {
                throw new InvalidOperationException("This entity already has parent.");
            }

            entity.parent = this;
        }

        private void RemoveInternal(Entity entity)
        {
            if (entity.Parent != this)
            {
                throw new InvalidOperationException("This entity is not a child of the expected parent.");
            }

            entity.parent = null;
        }

        private void AddInternal(EntityComponent component)
        {
            if (component.Entity != null)
            {
                throw new InvalidOperationException("An entity component cannot be set on more than one entity.");
            }

            if (component is TransformComponent transformComponent && Transform != transformComponent)
            {
                Components.Remove(Transform);
                Transform = transformComponent;
            }

            component.Entity = this;
        }

        private void RemoveInternal(EntityComponent component)
        {
            if (component.Entity != this)
            {
                throw new InvalidOperationException("This entity component is not set on this entity.");
            }

            component.Entity = null;
        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Entity entity in e.NewItems.Cast<Entity>())
                    {
                        AddInternal(entity);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Entity entity in e.OldItems.Cast<Entity>())
                    {
                        RemoveInternal(entity);
                    }
                    break;
            }
        }

        private void Components_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (EntityComponent component in e.NewItems.Cast<EntityComponent>())
                    {
                        AddInternal(component);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (EntityComponent component in e.OldItems.Cast<EntityComponent>())
                    {
                        RemoveInternal(component);
                    }
                    break;
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
