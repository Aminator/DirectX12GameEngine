using System;
using System.Linq;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12ComputeShaderSample
{
    public class MyComputeShader : ComputeShaderBase
    {
#nullable disable
        public WriteableStructuredBuffer<float> Destination;

        public StructuredBuffer<float> Source;
#nullable restore

        [ShaderMethod]
        [Shader("compute")]
        [NumThreads(100, 1, 1)]
        public override void CSMain(CSInput input)
        {
            Destination[input.DispatchThreadId.X] = Math.Max(Source[input.DispatchThreadId.X], 45);
        }
    }

    public class Test1
    {
        public static async Task RunAsync(GraphicsDevice device)
        {
            // Create graphics buffer

            int width = 10;
            int height = 10;

            float[] array = new float[width * height];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }

            Console.WriteLine("CPU Test:");
            CreateBuffers(device, array, out StructuredBuffer<float> cpuSourceBufferView, out WriteableStructuredBuffer<float> cpuDestinationBufferView);
            await ExecuteOnCpu(device, cpuSourceBufferView, cpuDestinationBufferView);
            Print(cpuDestinationBufferView);

            Console.WriteLine("GPU Test:");
            CreateBuffers(device, array, out StructuredBuffer<float> gpuSourceBufferView, out WriteableStructuredBuffer<float> gpuDestinationBufferView);
            await ExecuteOnGpu(device, gpuSourceBufferView, gpuDestinationBufferView);
            Print(gpuDestinationBufferView);

            void Print(WriteableStructuredBuffer<float> destination)
            {
                float[] outputArray = new float[width * height];

                Console.WriteLine("Before:");
                PrintMatrix(array, width, height);

                destination.Resource.GetData(outputArray.AsSpan());

                Console.WriteLine();
                Console.WriteLine("After:");
                PrintMatrix(outputArray, width, height);
            }
        }

        private static Task ExecuteOnCpu(GraphicsDevice device, StructuredBuffer<float> sourceBufferView, WriteableStructuredBuffer<float> destinationBufferView)
        {
            MyComputeShader myComputeShader = new MyComputeShader
            {
                Source = sourceBufferView,
                Destination = destinationBufferView
            };

            for (int x = 0; x < 100; x++)
            {
                myComputeShader.CSMain(new CSInput { DispatchThreadId = new Int3 { X = x } });
            }

            return Task.CompletedTask;
        }

        private static void CreateBuffers(GraphicsDevice device, float[] array, out StructuredBuffer<float> sourceBufferView, out WriteableStructuredBuffer<float> destinationBufferView)
        {
            GraphicsResource sourceBuffer = GraphicsResource.CreateBuffer<float>(device, array, ResourceFlags.None);
            GraphicsResource destinationBuffer = GraphicsResource.CreateBuffer<float>(device, array.Length * 2, ResourceFlags.AllowUnorderedAccess);
            sourceBufferView = new StructuredBuffer<float>(ShaderResourceView.FromBuffer<float>(sourceBuffer));
            destinationBufferView = new WriteableStructuredBuffer<float>(UnorderedAccessView.FromBuffer<float>(destinationBuffer));
        }

        private static async Task ExecuteOnGpu(GraphicsDevice device, StructuredBuffer<float> sourceBufferView, WriteableStructuredBuffer<float> destinationBufferView)
        {
            bool generateWithDelegate = false;

            DescriptorSet descriptorSet = new DescriptorSet(device, 2);
            descriptorSet.AddResourceViews(destinationBufferView);
            descriptorSet.AddResourceViews(sourceBufferView);

            // Generate computer shader
            ShaderGenerator shaderGenerator = generateWithDelegate
                ? CreateShaderGeneratorWithDelegate(sourceBufferView, destinationBufferView)
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
        }

        private static ShaderGenerator CreateShaderGeneratorWithClass()
        {
            MyComputeShader myComputeShader = new MyComputeShader();

            return new ShaderGenerator(myComputeShader);
        }

        [AnonymousShaderMethod(0)]
        private static ShaderGenerator CreateShaderGeneratorWithDelegate(StructuredBuffer<float> sourceBuffer, WriteableStructuredBuffer<float> destinationBuffer)
        {
            Action<CSInput> action = input =>
            {
                destinationBuffer[input.DispatchThreadId.X] = Math.Max(sourceBuffer[input.DispatchThreadId.X], 45);
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
