using System;

namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IMaterialDescriptor : IComputeShader
    {
        Guid Id { get; set; }

        MaterialAttributes Attributes { get; set; }
    }
}
