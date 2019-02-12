using System;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Rendering.Shaders
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ShaderMethodAttribute : ShaderResourceAttribute
    {
        public ShaderMethodAttribute([CallerLineNumber] int order = 0) : base(order)
        {
        }
    }

    public class ShaderAttribute : ShaderMethodAttribute
    {
        public ShaderAttribute(string name, [CallerLineNumber] int order = 0) : base(order)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
