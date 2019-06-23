using System.Numerics;
using DirectX12GameEngine.Rendering.Lights;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(LightSystem))]
    public sealed class LightComponent : EntityComponent
    {
        public float Intensity { get; set; } = 1.0f;

        public ILight Light { get; set; } = new DirectionalLight();

        internal Vector3 Position { get; set; }

        internal Vector3 Direction { get; set; }

        internal Vector3 Color { get; set; }

        public void Update()
        {
            if (Entity is null) return;

            Position = Entity.Transform.WorldMatrix.Translation;

            Vector3 lightDirection = Vector3.TransformNormal(-Vector3.UnitZ, Entity.Transform.WorldMatrix);
            Direction = Vector3.Normalize(lightDirection);

            Color = (Light as IColorLight)?.ComputeColor(Intensity) ?? default;
        }
    }
}
