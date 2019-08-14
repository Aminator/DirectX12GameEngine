using System;

namespace DirectX12GameEngine.Shaders
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NumThreadsAttribute : Attribute
    {
        public NumThreadsAttribute(uint x, uint y, uint z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public uint X { get; }

        public uint Y { get; }

        public uint Z { get; }
    }
}
