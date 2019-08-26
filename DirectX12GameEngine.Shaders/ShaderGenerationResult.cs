using System.Collections.Generic;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGenerationResult
    {
        public ShaderGenerationResult(string shaderSource)
        {
            ShaderSource = shaderSource;
        }

        public string ShaderSource { get; }

        public Dictionary<string, string> EntryPoints { get; } = new Dictionary<string, string>();
    }
}
