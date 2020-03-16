using System.Collections;
using System.Collections.Generic;
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
        private Matrix4x4 localMatrix = Matrix4x4.Identity;
        private Matrix4x4 worldMatrix = Matrix4x4.Identity;

        private Vector3 position;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = Vector3.One;

        public event PropertyChangedEventHandler? PropertyChanged;

        public IEnumerable<TransformComponent> Children
        {
            get
            {
                if (Entity is null) yield break;

                foreach (Entity entity in Entity.Children)
                {
                    if (entity.Transform != null)
                    {
                        yield return entity.Transform;
                    }
                }
            }
        }

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TransformComponent? Parent { get => Entity?.Parent?.Transform; set { if (Entity != null) Entity.Parent = value?.Entity; } }

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ref Matrix4x4 LocalMatrix => ref localMatrix;

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ref Matrix4x4 WorldMatrix => ref worldMatrix;

        public Vector3 Position { get => position; set => Set(ref position, value); }
               
        public Quaternion Rotation { get => rotation; set => Set(ref rotation, value); }

        public Vector3 Scale { get => scale; set => Set(ref scale, value); }

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3 RotationEuler { get => Rotation.ToEuler(); set => Rotation = value.ToQuaternion(); }

        public override string ToString() => $"Position: {Position}, Rotation: {RotationEuler}, Scale: {Scale}";

        public IEnumerator<TransformComponent> GetEnumerator() => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void UpdateLocalMatrix()
        {
            LocalMatrix = Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(Rotation)
                * Matrix4x4.CreateTranslation(Position);
        }

        public void UpdateLocalFromWorldMatrix()
        {
            if (Parent is null)
            {
                LocalMatrix = WorldMatrix;
            }
            else
            {
                if (Matrix4x4.Invert(Parent.WorldMatrix, out Matrix4x4 inverseParentMatrix))
                {
                    LocalMatrix = WorldMatrix * inverseParentMatrix;
                }
            }

            (Scale, Rotation, Position) = LocalMatrix;
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

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }
    }
}
