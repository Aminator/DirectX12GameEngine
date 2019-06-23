using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;
using System.Runtime.Serialization;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(TransformSystem))]
    public sealed class TransformComponent : EntityComponent, IEnumerable<TransformComponent>
    {
        private TransformComponent? parent;

        private Vector3 position;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = Vector3.One;

        public TransformComponent()
        {
            ChildEntities = new TransformCollectionWrapper(Children);
            Children.CollectionChanged += Children_CollectionChanged;
        }

        [IgnoreDataMember]
        public ObservableCollection<TransformComponent> Children { get; } = new ObservableCollection<TransformComponent>();

        public IList<Entity> ChildEntities { get; }

        [IgnoreDataMember]
        public Matrix4x4 LocalMatrix { get; set; } = Matrix4x4.Identity;

        [IgnoreDataMember]
        public Matrix4x4 WorldMatrix { get; set; } = Matrix4x4.Identity;

        public Vector3 Position { get => position; set => position = value; }

        public Quaternion Rotation { get => rotation; set => rotation = value; }

        public Vector3 Scale { get => scale; set => scale = value; }

        [IgnoreDataMember]
        public Vector3 RotationEuler { get => QuaternionExtensions.ToEuler(Rotation); set => Rotation = QuaternionExtensions.ToQuaternion(value); }

        internal bool IsMovingInsideRootScene { get; private set; }

        [IgnoreDataMember]
        public TransformComponent? Parent
        {
            get => parent;
            set
            {
                TransformComponent? oldParent = parent;

                if (oldParent == value) return;

                Scene? previousScene = oldParent?.Entity?.Scene;
                Scene? newScene = value?.Entity?.Scene;

                while (previousScene?.Parent != null)
                {
                    previousScene = previousScene.Parent;
                }

                while (newScene?.Parent != null)
                {
                    newScene = newScene.Parent;
                }

                bool isMoving = newScene != null && newScene == previousScene;

                if (isMoving)
                {
                    IsMovingInsideRootScene = true;
                }

                oldParent?.Children.Remove(this);
                value?.Children.Add(this);

                if (isMoving)
                {
                    IsMovingInsideRootScene = false;
                }
            }
        }

        public IEnumerator<TransformComponent> GetEnumerator() => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void UpdateLocalMatrix()
        {
            LocalMatrix = Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(Rotation)
                * Matrix4x4.CreateTranslation(Position);
        }

        public void UpdateWorldMatrix()
        {
            UpdateLocalMatrix();
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

                WorldMatrix = LocalMatrix * Parent.WorldMatrix;
            }
            else
            {
                WorldMatrix = LocalMatrix;

                Scene? scene = Entity?.Scene;

                if (scene != null)
                {
                    if (recursive)
                    {
                        scene.UpdateWorldMatrix();
                    }

                    WorldMatrix *= scene.WorldMatrix;
                }
            }
        }

        private void AddInternal(TransformComponent transformComponent)
        {
            if (transformComponent.Parent != null)
            {
                throw new InvalidOperationException("This transform component already has parent.");
            }

            transformComponent.parent = this;
        }

        private void RemoveInternal(TransformComponent transformComponent)
        {
            if (transformComponent.Parent != this)
            {
                throw new InvalidOperationException("This transform component is not a child of the expected parent.");
            }

            transformComponent.parent = null;
        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (TransformComponent transformComponent in e.NewItems)
                    {
                        AddInternal(transformComponent);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (TransformComponent transformComponent in e.OldItems)
                    {
                        RemoveInternal(transformComponent);
                    }
                    break;
            }
        }
    }
}
