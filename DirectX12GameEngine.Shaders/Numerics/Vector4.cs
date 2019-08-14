using System;

namespace DirectX12GameEngine.Shaders.Numerics
{
    public struct Vector4
    {
        public float this[int i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static float Length(System.Numerics.Vector4 value) => value.Length();

        public static implicit operator System.Numerics.Vector4(Vector4 value) => throw new NotImplementedException();

        public static implicit operator Vector4(System.Numerics.Vector4 value) => throw new NotImplementedException();
    }
}
