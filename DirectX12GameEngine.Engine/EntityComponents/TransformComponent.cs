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
        private Vector3 position;
        private Quaternion rotation = Quaternion.Identity;
        private Vector3 scale = Vector3.One;

        public event PropertyChangedEventHandler? PropertyChanged;

        public IEnumerable<TransformComponent> Children => this;

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TransformComponent? Parent { get => Entity?.Parent?.Transform; set { if (Entity != null) Entity.Parent = value?.Entity; } }

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Matrix4x4 LocalMatrix { get; set; } = Matrix4x4.Identity;

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Matrix4x4 WorldMatrix { get; set; } = Matrix4x4.Identity;

        public Vector3 Position { get => position; set => Set(ref position, value); }

        public Quaternion Rotation { get => rotation; set => Set(ref rotation, value); }

        public Vector3 Scale { get => scale; set => Set(ref scale, value); }

        [IgnoreDataMember]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Vector3 RotationEuler { get => Rotation.ToEuler(); set { if (Set(ref rotation, value.ToQuaternion(), nameof(Rotation))) NotifyPropertyChanged(); } }

        public override string ToString() => $"Position: {Position}, Rotation: {RotationEuler}, Scale: {Scale}";

        public IEnumerator<TransformComponent> GetEnumerator()
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
