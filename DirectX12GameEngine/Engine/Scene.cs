using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace DirectX12GameEngine.Engine
{
    public sealed class Scene : ObservableCollection<Entity>
    {
        public Scene()
        {
            CollectionChanged += Entities_CollectionChanged;
        }

        public ObservableCollection<Entity> Entities => this;

        public Guid Id { get; } = Guid.NewGuid();

        private void AddInternal(Entity entity)
        {
            if (entity.Scene != null)
            {
                throw new InvalidOperationException("An entity cannot be set on more than one scene.");
            }

            entity.Scene = this;
        }

        private void RemoveInternal(Entity entity)
        {
            if (entity.Scene != this)
            {
                throw new InvalidOperationException("This entity is not in this scene.");
            }

            entity.Scene = null;
        }

        private void Entities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Entity entity in e.NewItems)
                    {
                        AddInternal(entity);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Entity entity in e.OldItems)
                    {
                        RemoveInternal(entity);
                    }
                    break;
            }
        }
    }
}
