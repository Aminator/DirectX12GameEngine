using System;
using System.Numerics;

namespace DirectX12GameEngine.Shaders.Numerics
{
    public struct GraphicsVector4
    {
        public float this[int i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static float Length(Vector4 value) => value.Length();

        public static implicit operator Vector4(GraphicsVector4 value) => throw new NotImplementedException();

        public static implicit operator GraphicsVector4(Vector4 value) => throw new NotImplementedException();
    }
}
