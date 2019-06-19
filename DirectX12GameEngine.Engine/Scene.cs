using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;
using System.Xml.Serialization;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Engine
{
    public sealed class Scene : ObservableCollection<Entity>, IIdentifiable
    {
        private Scene? parent;

        public Scene()
        {
            CollectionChanged += Entities_CollectionChanged;
            Children.CollectionChanged += Children_CollectionChanged;
        }

        [XmlIgnore]
        public ObservableCollection<Entity> Entities => this;

        public ObservableCollection<Scene> Children { get; } = new ObservableCollection<Scene>();

        public Guid Id { get; set; } = Guid.NewGuid();

        public Vector3 Offset { get; set; }

        [XmlIgnore]
        public Matrix4x4 WorldMatrix { get; private set; }

        [XmlIgnore]
        public Scene? Parent
        {
            get => parent;
            set
            {
                Scene? oldParent = parent;

                if (oldParent == value) return;

                oldParent?.Children.Remove(this);
                value?.Children.Add(this);
            }
        }

        public void UpdateWorldMatrix()
        {
            UpdateWorldMatrixInternal(true);
        }

        internal void UpdateWorldMatrixInternal(bool recursive)
        {
            if (Parent != null)
            {
                if (recursive)
                {
                    Parent.UpdateWorldMatrix();
                }

                WorldMatrix = Parent.WorldMatrix;
            }
            else
            {
                WorldMatrix = Matrix4x4.Identity;
            }

            WorldMatrix *= Matrix4x4.CreateTranslation(Offset);
        }

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

        private void AddInternal(Scene scene)
        {
            if (scene.Parent != null)
            {
                throw new InvalidOperationException("This scene already has parent.");
            }

            scene.parent = this;
        }

        private void RemoveInternal(Scene scene)
        {
            if (scene.Parent != this)
            {
                throw new InvalidOperationException("This scene is not a child of the expected parent.");
            }

            scene.parent = null;
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

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Scene scene in e.NewItems)
                    {
                        AddInternal(scene);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Scene scene in e.OldItems)
                    {
                        RemoveInternal(scene);
                    }
                    break;
            }
        }
    }
}
