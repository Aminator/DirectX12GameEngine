using System;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;
using SharpDX.Direct3D12;

using Buffer = DirectX12GameEngine.Graphics.Buffer;
using CommandList = DirectX12GameEngine.Graphics.CommandList;
using CommandListType = DirectX12GameEngine.Graphics.CommandListType;
using PipelineState = DirectX12GameEngine.Graphics.PipelineState;

namespace DirectX12ComputeShaderSample
{
    namespace Test
    {
        public class MyClass
        {
            public static readonly int MyStaticInt;
        }
    }

    public class MyComputeShader
    {
        [ConstantBuffer]
        public float[] Source;

        [ShaderMember]
        public RWBufferResource<float> Data;

        [ShaderMember]
        [Shader("compute")]
        [NumThreads(100, 1, 1)]
        public void CSMain([SystemDispatchThreadIdSemantic] UInt3 id)
        {
            Data[id.X] = Source[id.X];
        }
    }

    public class Program
    {
        private static async Task Main()
        {
            // Generate computer shader

            MyComputeShader myComputeShader = new MyComputeShader();
            ShaderGenerator shaderGenerator = new ShaderGenerator(myComputeShader);
            ShaderGenerationResult result = shaderGenerator.GenerateShader();

            byte[] shaderBytecode = ShaderCompiler.CompileShader(result.ShaderSource, SharpDX.D3DCompiler.ShaderVersion.ComputeShader, result.ComputeShader);

            // Create graphics device and pipeline state

            using GraphicsDevice device = new GraphicsDevice(SharpDX.Direct3D.FeatureLevel.Level_12_1);

            RootParameter[] rootParameters = new RootParameter[]
            {
                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 0)),
                new RootParameter(ShaderVisibility.All, new DescriptorRange(DescriptorRangeType.UnorderedAccessView, 1, 0))
            };

            var rootSignatureDescription = new RootSignatureDescription(RootSignatureFlags.None, rootParameters);
            var rootSignature = device.CreateRootSignature(rootSignatureDescription);

            PipelineState pipelineState = new PipelineState(device, rootSignature, shaderBytecode);

            // Create graphics buffer

            int width = 10;
            int height = 10;

            float[] array = new float[width * height * 4];

            for (int i = 0, j = 0; i < width * height; i++, j += 4)
            {
                array[j] = i;
            }

            float[] outputArray = new float[width * height];

            using Buffer<float> sourceBuffer = Buffer.Constant.New(device, array.AsSpan());
            using Buffer<float> destinationBuffer = Buffer.UnorderedAccess.New<float>(device, array.Length);

            // Execute computer shader

            using (CommandList commandList = new CommandList(device, CommandListType.Compute))
            {
                commandList.SetPipelineState(pipelineState);

                commandList.SetComputeRootDescriptorTable(0, sourceBuffer);
                commandList.SetComputeRootDescriptorTable(1, destinationBuffer);

                commandList.Dispatch(1, 1, 1);
                await commandList.FlushAsync();
            }

            // Print matrix

            Console.WriteLine("Before:");
            PrintMatrix(array, width, height);

            destinationBuffer.GetData(outputArray.AsSpan());

            Console.WriteLine();
            Console.WriteLine("After:");
            PrintMatrix(outputArray, width, height);
        }

        private static void PrintMatrix(float[] array, int width, int height)
        {
            int numberWidth = array.Max().ToString().Length;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Console.Write(array[x + y * width].ToString().PadLeft(numberWidth));
                    Console.Write(", ");
                }

                Console.WriteLine();
            }
        }
    }
}

