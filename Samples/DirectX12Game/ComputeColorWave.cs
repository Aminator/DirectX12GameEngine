using System;
using System.Numerics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12Game
{
    [StaticResource]
    public class ComputeColorWave : IColorShader
    {
        public void Accept(ShaderGeneratorContext context)
        {
            Amplitude.Accept(context);
            Frequency.Accept(context);
            Speed.Accept(context);
        }

        public IScalarShader Amplitude { get; set; } = new ScalarShader(1.0f);

        public IScalarShader Frequency { get; set; } = new ScalarShader(10.0f);

        public IScalarShader Speed { get; set; } = new ScalarShader(0.05f);

        [ShaderMethod]
        public Vector4 ComputeColor(in SamplingContext context)
        {
            float phase = GraphicsVector2.Length(context.TextureCoordinate - new Vector2(0.5f, 0.5f));
            float value = (float)Math.Sin((phase + /*Global.TotalTime **/ Speed.ComputeScalar(context)) * 2.0f * 3.14f * Frequency.ComputeScalar(context)) * Amplitude.ComputeScalar(context);
            value = value * 0.5f + 0.5f;

            return new Vector4(value, value, value, 1.0f);
        }
    }
}
