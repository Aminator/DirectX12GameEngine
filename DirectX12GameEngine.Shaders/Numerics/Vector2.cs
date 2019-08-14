using System;

namespace DirectX12GameEngine.Shaders.Numerics
{
    public struct Vector2
    {
        public float this[int i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static float Length(System.Numerics.Vector2 value) => value.Length();

        public static implicit operator System.Numerics.Vector2(Vector2 value) => throw new NotImplementedException();

        public static implicit operator Vector2(System.Numerics.Vector2 value) => throw new NotImplementedException();
    }
}
