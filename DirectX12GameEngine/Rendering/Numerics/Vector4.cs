using System;

namespace DirectX12GameEngine.Rendering.Numerics
{
    public struct Vector4
    {
        private readonly Memory<float> memory;

        public Vector4(float x, float y, float z, float w)
        {
            memory = new float[] { x, y, z, w };
        }

        public float this[int i]
        {
            get => memory.Span[i];
            set => memory.Span[i] = value;
        }

        public static implicit operator System.Numerics.Vector4(Vector4 value)
        {
            return new System.Numerics.Vector4(value[0], value[1], value[2], value[3]);
        }

        public static implicit operator Vector4(System.Numerics.Vector4 value)
        {
            return new Vector4(value.X, value.Y, value.Z, value.W);
        }
    }
}
