using System;

namespace DirectX12GameEngine.Shaders
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class ShaderTypeAttribute : Attribute
    {
        public ShaderTypeAttribute(string typeName)
        {
            TypeName = typeName;
        }

        public string TypeName { get; }
    }
}
