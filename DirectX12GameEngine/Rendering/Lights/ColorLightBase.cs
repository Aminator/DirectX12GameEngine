using System.Numerics;

namespace DirectX12GameEngine.Rendering.Lights
{
    public abstract class ColorLightBase : IColorLight
    {
        public Vector3 Color { get; set; } = Vector3.One;

        public Vector3 ComputeColor(float intensity)
        {
            return Color * intensity;
        }
    }
}
