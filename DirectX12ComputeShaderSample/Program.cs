using System;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;
using Vortice.DirectX.Direct3D12;
using Vortice.Dxc;

using Buffer = DirectX12GameEngine.Graphics.Buffer;
using CommandListType = DirectX12GameEngine.Graphics.CommandListType;
using DescriptorHeapType = DirectX12GameEngine.Graphics.DescriptorHeapType;

namespace DirectX12ComputeShaderSample
{
    public class MyComputeShader : ComputeShaderBase
    {
        public StructuredBufferResource<float> Source;

        public RWStructuredBufferResource<float> Destination;

        [ShaderMember]
        [Shader("compute")]
        [NumThreads(100, 1, 1)]
        public override void CSMain(CSInput input)
        {
            Destination[input.DispatchThreadId.X] = Math.Max(Source[input.DispatchThreadId.X], 45);
        }
    }

    public class Program
    {
        private static async Task Main()
        {
            // Create graphics device

            using GraphicsDevice device = new GraphicsDevice(FeatureLevel.Level12_1);

            // Create graphics buffer

            int width = 10;
            int height = 10;

            float[] array = new float[width * height];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }

            float[] outputArray = new float[width * height];

            using Buffer<float> sourceBuffer = Buffer.ShaderResource.New(device, array.AsSpan());
            using Buffer<float> destinationBuffer = Buffer.UnorderedAccess.New<float>(device, array.Length);

            DescriptorSet descriptorSet = new DescriptorSet(device, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, 2);
            descriptorSet.AddDescriptor(sourceBuffer);
            descriptorSet.AddDescriptor(destinationBuffer);

            // Generate computer shader

            //StructuredBufferResource<float> source = sourceBuffer.GetStructuredBuffer();
            //RWStructuredBufferResource<float> destination = destinationBuffer.GetRWStructuredBuffer();

            //Action<CSInput> action = input =>
            //{
            //    destination[input.DispatchThreadId.X] = source[input.DispatchThreadId.X];
            //};

            //ShaderGenerator shaderGenerator = new ShaderGenerator(action);
            //ShaderGenerationResult result = shaderGenerator.GenerateShader();

            MyComputeShader myComputeShader = new MyComputeShader();

            ShaderGenerator shaderGenerator = new ShaderGenerator(myComputeShader);
            ShaderGenerationResult result = shaderGenerator.GenerateShader();

            byte[] shaderBytecode = ShaderCompiler.Compile(DxcShaderStage.ComputeShader, result.ShaderSource, result.EntryPoints["compute"]);

            DescriptorRange1[] descriptorRanges = new DescriptorRange1[]
            {
                new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, 0),
                new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 1, 0)
            };

            RootParameter1 rootParameter = new RootParameter1(new RootDescriptorTable1(descriptorRanges), ShaderVisibility.All);

            var rootSignatureDescription = new VersionedRootSignatureDescription(new RootSignatureDescription1(RootSignatureFlags.None, new[] { rootParameter }));
            var rootSignature = device.CreateRootSignature(rootSignatureDescription);

            PipelineState pipelineState = new PipelineState(device, rootSignature, shaderBytecode);

            // Execute computer shader

            using (CommandList commandList = new CommandList(device, CommandListType.Compute))
            {
                commandList.SetPipelineState(pipelineState);

                commandList.SetComputeRootDescriptorTable(0, descriptorSet);

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

