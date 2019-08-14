using System;

namespace DirectX12GameEngine.Shaders
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ShaderAttribute : Attribute
    {
        public ShaderAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
