using System.Collections.Generic;

namespace DirectX12GameEngine.Rendering
{
    public class CompiledShader
    {
        public Dictionary<string, byte[]> Shaders { get; } = new Dictionary<string, byte[]>();
    }
}
