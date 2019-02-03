using System.Collections.Generic;

namespace DirectX12GameEngine
{
    public sealed class Model
    {
        public IList<Material> Materials { get; } = new List<Material>();

        public IList<Mesh> Meshes { get; } = new List<Mesh>();
    }
}
