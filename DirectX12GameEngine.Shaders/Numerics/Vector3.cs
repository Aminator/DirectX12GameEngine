using System;

namespace DirectX12GameEngine.Shaders.Numerics
{
    public struct Vector3
    {
        public float this[int i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static float Length(System.Numerics.Vector3 value) => value.Length();

        public static implicit operator System.Numerics.Vector3(Vector3 value) => throw new NotImplementedException();

        public static implicit operator Vector3(System.Numerics.Vector3 value) => throw new NotImplementedException();
    }
}
