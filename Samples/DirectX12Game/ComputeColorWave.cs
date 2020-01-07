using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Materials;
using DirectX12GameEngine.Shaders;

namespace DirectX12Game
{
    [StaticResource]
    public class ComputeColorWave : IComputeColor
    {
        public void Visit(MaterialGeneratorContext context)
        {
            Amplitude.Visit(context);
            Frequency.Visit(context);
            Speed.Visit(context);
        }

        public IComputeScalar Amplitude { get; set; } = new ComputeScalar(1.0f);

        public IComputeScalar Frequency { get; set; } = new ComputeScalar(10.0f);

        public IComputeScalar Speed { get; set; } = new ComputeScalar(0.05f);

        [ShaderMember]
        [ShaderMethod]
        public Vector4 Compute()
        {
            float phase = DirectX12GameEngine.Shaders.Numerics.Vector2.Length(Texturing.TexCoord - new Vector2(0.5f, 0.5f));
            float value = (float)Math.Sin((phase + Global.TotalTime * Speed.Compute()) * 2.0f * 3.14f * Frequency.Compute()) * Amplitude.Compute();
            value = value * 0.5f + 0.5f;

            return new Vector4(value, value, value, 1.0f);
        }
    }
}
