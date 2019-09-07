using System.Collections.Generic;
using System.ComponentModel;
using DirectX12GameEngine.Core.Assets;

namespace DirectX12GameEngine.Rendering
{
    [TypeConverter(typeof(AssetReferenceTypeConverter))]
    public sealed class Model
    {
        public IList<Material> Materials { get; } = new List<Material>();

        public IList<Mesh> Meshes { get; } = new List<Mesh>();
    }
}
