using System.Collections.Generic;

namespace DirectX12GameEngine
{
    public sealed class Model
    {
        public List<Material> Materials { get; } = new List<Material>();

        public List<Mesh> Meshes { get; } = new List<Mesh>();

        internal CompiledCommandList? CommandList { get; set; }

        internal Texture[]? ConstantBuffers { get; set; }
    }
}
