using System.Numerics;

namespace DirectX12GameEngine.Rendering.Lights
{
    public interface IColorLight : ILight
    {
        Vector3 Color { get; set; }

        Vector3 ComputeColor(float intensity);
    }
}
