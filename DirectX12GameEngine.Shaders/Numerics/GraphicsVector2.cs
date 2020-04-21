using System;
using System.Numerics;

namespace DirectX12GameEngine.Shaders.Numerics
{
    public struct GraphicsVector2
    {
        public float this[int i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static float Length(Vector2 value) => value.Length();

        public static implicit operator Vector2(GraphicsVector2 value) => throw new NotImplementedException();

        public static implicit operator GraphicsVector2(Vector2 value) => throw new NotImplementedException();
    }
}
