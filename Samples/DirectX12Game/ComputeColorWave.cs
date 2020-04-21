using System;
using System.Numerics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Materials;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12Game
{
    [StaticResource]
    public class ComputeColorWave : IComputeColor
    {
        public void Accept(ShaderGeneratorContext context)
        {
            Amplitude.Accept(context);
            Frequency.Accept(context);
            Speed.Accept(context);
        }

        public IComputeScalar Amplitude { get; set; } = new ComputeScalar(1.0f);

        public IComputeScalar Frequency { get; set; } = new ComputeScalar(10.0f);

        public IComputeScalar Speed { get; set; } = new ComputeScalar(0.05f);

        [ShaderMethod]
        public Vector4 Compute()
        {
            float phase = GraphicsVector2.Length(Texturing.TexCoord - new Vector2(0.5f, 0.5f));
            float value = (float)Math.Sin((phase + Global.TotalTime * Speed.Compute()) * 2.0f * 3.14f * Frequency.Compute()) * Amplitude.Compute();
            value = value * 0.5f + 0.5f;

            return new Vector4(value, value, value, 1.0f);
        }
    }
}
