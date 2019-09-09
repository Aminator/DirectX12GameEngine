using System.Collections.Generic;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGeneratorResult
    {
        public ShaderGeneratorResult(string shaderSource)
        {
            ShaderSource = shaderSource;
        }

        public string ShaderSource { get; }

        public Dictionary<string, string> EntryPoints { get; } = new Dictionary<string, string>();
    }
}
