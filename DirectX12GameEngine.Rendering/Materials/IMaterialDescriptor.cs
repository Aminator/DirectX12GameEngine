using System;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialDescriptor : IShader
    {
        Guid Id { get; set; }

        MaterialAttributes Attributes { get; set; }
    }
}
