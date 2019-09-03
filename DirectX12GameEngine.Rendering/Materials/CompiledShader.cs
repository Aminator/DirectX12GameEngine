using System.Collections.Generic;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class CompiledShader
    {
        public Dictionary<string, byte[]> Shaders { get; } = new Dictionary<string, byte[]>();
    }
}
