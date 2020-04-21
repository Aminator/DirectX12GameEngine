using System;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12ComputeShaderSample
{
    public class MyComputeShader : ComputeShaderBase
    {
#nullable disable
        public RWStructuredBufferResource<float> Destination;

        public StructuredBufferResource<float> Source;
#nullable restore

        [ShaderMethod]
        [Shader("compute")]
        [NumThreads(100, 1, 1)]
        public override void CSMain(CSInput input)
        {
            Destination[input.DispatchThreadId.X] = Math.Max(Source[input.DispatchThreadId.X], 45);
        }
    }

    public static class GraphicsBufferExtensions
    {
        public static StructuredBufferResource<T> GetStructuredBuffer<T>(this GraphicsBuffer<T> buffer) where T : unmanaged
        {
            return new StructuredBufferResource<T>();
        }

        public static RWStructuredBufferResource<T> GetRWStructuredBuffer<T>(this GraphicsBuffer<T> buffer) where T : unmanaged
        {
            return new RWStructuredBufferResource<T>();
        }
    }

    public class Test1
    {
        public static async Task RunAsync(GraphicsDevice device)
        {
            bool generateWithDelegate = false;

            // Create graphics buffer

            int width = 10;
            int height = 10;

            float[] array = new float[width * height];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }

            float[] outputArray = new float[width * height];

            using GraphicsBuffer<float> sourceBuffer = GraphicsBuffer.Create<float>(device, array, ResourceFlags.None);
            using GraphicsBuffer<float> destinationBuffer = GraphicsBuffer.Create<float>(device, array.Length * 2, ResourceFlags.AllowUnorderedAccess);

            GraphicsBuffer<float> slicedDestinationBuffer = destinationBuffer.Slice(20, 60);
            slicedDestinationBuffer = slicedDestinationBuffer.Slice(10, 50);

            DescriptorSet descriptorSet = new DescriptorSet(device, 2);
            descriptorSet.AddUnorderedAccessViews(slicedDestinationBuffer);
            descriptorSet.AddShaderResourceViews(sourceBuffer);

            // Generate computer shader
            ShaderGenerator shaderGenerator = generateWithDelegate
                ? CreateShaderGeneratorWithDelegate(sourceBuffer, destinationBuffer)
                : CreateShaderGeneratorWithClass();

            ShaderGeneratorResult result = shaderGenerator.GenerateShader();

            // Compile shader

            byte[] shaderBytecode = ShaderCompiler.Compile(ShaderStage.ComputeShader, result.ShaderSource, result.EntryPoints["compute"]);

            DescriptorRange[] descriptorRanges = new DescriptorRange[]
            {
                new DescriptorRange(DescriptorRangeType.UnorderedAccessView, 1, 0),
                new DescriptorRange(DescriptorRangeType.ShaderResourceView, 1, 0)
            };

            RootParameter rootParameter = new RootParameter(new RootDescriptorTable(descriptorRanges), ShaderVisibility.All);

            RootSignatureDescription rootSignatureDescription = new RootSignatureDescription(RootSignatureFlags.None, new[] { rootParameter });
            RootSignature rootSignature = new RootSignature(device, rootSignatureDescription);
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

        private static ShaderGenerator CreateShaderGeneratorWithClass()
        {
            MyComputeShader myComputeShader = new MyComputeShader();

            return new ShaderGenerator(myComputeShader);
        }

        [AnonymousShaderMethod(0)]
        private static ShaderGenerator CreateShaderGeneratorWithDelegate(GraphicsBuffer<float> sourceBuffer, GraphicsBuffer<float> destinationBuffer)
        {
            StructuredBufferResource<float> source = sourceBuffer.GetStructuredBuffer();
            RWStructuredBufferResource<float> destination = destinationBuffer.GetRWStructuredBuffer();

            Action<CSInput> action = input =>
            {
                destination[input.DispatchThreadId.X] = Math.Max(source[input.DispatchThreadId.X], 45);
            };

            return new ShaderGenerator(action, new ShaderAttribute("compute"), new NumThreadsAttribute(100, 1, 1));
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
