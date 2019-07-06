using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using DirectX12GameEngine.Core;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(TransformSystem))]
    public sealed class TransformComponent : EntityComponent, IEnumerable<TransformComponent>, INotifyPropertyChanged
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

        public event PropertyChangedEventHandler PropertyChanged;

        [IgnoreDataMember]
        public ObservableCollection<TransformComponent> Children { get; } = new ObservableCollection<TransformComponent>();

        public IList<Entity> ChildEntities { get; }

        [IgnoreDataMember]
        public Matrix4x4 LocalMatrix { get; set; } = Matrix4x4.Identity;

        [IgnoreDataMember]
        public Matrix4x4 WorldMatrix { get; set; } = Matrix4x4.Identity;

        public Vector3 Position { get => position; set => Set(ref position, value); }

        public Quaternion Rotation { get => rotation; set => Set(ref rotation, value); }

        public Vector3 Scale { get => scale; set => Set(ref scale, value); }

        [IgnoreDataMember]
        public Vector3 RotationEuler { get => QuaternionExtensions.ToEuler(Rotation); set { if (Set(ref rotation, QuaternionExtensions.ToQuaternion(value), nameof(Rotation))) NotifyPropertyChanged(); } }

        [IgnoreDataMember]
        public TransformComponent? Parent
        {
            get => parent;
            set
            {
                TransformComponent? oldParent = parent;

                if (oldParent == value) return;

                oldParent?.Children.Remove(this);
                value?.Children.Add(this);
            }
        }

        public override string ToString() => $"Position: {Position}, Rotation: {RotationEuler}, Scale: {Scale}";

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
            if (Parent is null)
            {
                WorldMatrix = LocalMatrix;
            }
            else
            {
                if (recursive)
                {
                    Parent.UpdateWorldMatrix();
                }

                WorldMatrix = LocalMatrix * Parent.WorldMatrix;
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
