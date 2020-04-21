using System;
using System.Numerics;

namespace DirectX12GameEngine.Shaders.Numerics
{
    public struct GraphicsVector3
    {
        public float this[int i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static float Length(Vector3 value) => value.Length();

        public static implicit operator Vector3(GraphicsVector3 value) => throw new NotImplementedException();

        public static implicit operator GraphicsVector3(Vector3 value) => throw new NotImplementedException();
    }
}
