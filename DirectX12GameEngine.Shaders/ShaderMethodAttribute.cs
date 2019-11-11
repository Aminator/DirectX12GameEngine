using System;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderMethodAttribute : Attribute
    {
        public ShaderMethodAttribute(string? shaderSource = null, params Type[] dependentTypes)
        {
            ShaderSource = shaderSource;
            DependentTypes = dependentTypes;
        }

        public string? ShaderSource { get; }

        public Type[] DependentTypes { get; }
    }
}
