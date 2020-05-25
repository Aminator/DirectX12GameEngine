using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12ComputeShaderSample
{
    public class Test2
    {
        [ShaderMethod]
        public static float Sigmoid(float x)
        {
            return 1 / (1 + (float)Math.Exp(-x));
        }

        public static async Task RunAsync(GraphicsDevice device)
        {
            int count = 100;
            int sumCount = 10;

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < count; i++)
            {
                int index = i;

                Console.WriteLine($"Scheduling task {index}");

                tasks.Add(Task.Run(async () =>
                {
                    TestShader cpuShader = CreateTestShader(device, sumCount);
                    await ExecuteOnCpu(device, index, cpuShader);

                    TestShader gpuShader = CreateTestShader(device, sumCount);
                    await ExecuteOnGpu(device, index, gpuShader);
                }));
            }

            Console.WriteLine("Awaiting tasks...");
            Console.WriteLine($"Task count: {tasks.Count}");

            await Task.WhenAll(tasks);

            Console.WriteLine("DONE!");
        }

        private static TestShader CreateTestShader(GraphicsDevice device, int sumCount)
        {
            GraphicsResource buffer = GraphicsResource.CreateBuffer<float>(device, sumCount, ResourceFlags.AllowUnorderedAccess);
            WriteableStructuredBuffer<float> bufferView = new WriteableStructuredBuffer<float>(UnorderedAccessView.FromBuffer<float>(buffer));

            TestShader shader = new TestShader(bufferView, Sigmoid);
            return shader;
        }

        private static Task ExecuteOnCpu(GraphicsDevice device, int index, TestShader shader)
        {
            for (int x = 0; x < 10; x++)
            {
                shader.Execute(new CSInput { DispatchThreadId = new Int3 { X = x } });
            }

            float sum = shader.DestinationBuffer.Resource.GetArray<float>().Sum();

            Console.WriteLine($"Origin: CPU, Thread: {index}, sum: {sum}.");

            return Task.CompletedTask;
        }

        private static async Task ExecuteOnGpu(GraphicsDevice device, int index, TestShader shader)
        {
            ShaderGeneratorContext context = new ShaderGeneratorContext(device);
            context.Visit(shader);

            PipelineState pipelineState = await context.CreateComputePipelineStateAsync();
            DescriptorSet? descriptorSet = context.CreateShaderResourceViewDescriptorSet();

            using (CommandList commandList = new CommandList(device, CommandListType.Compute))
            {
                commandList.SetPipelineState(pipelineState);

                if (descriptorSet != null)
                {
                    commandList.SetComputeRootDescriptorTable(0, descriptorSet);
                }

                commandList.Dispatch(1, 1, 1);
                await commandList.FlushAsync();
            }

            float sum = shader.DestinationBuffer.Resource.GetArray<float>().Sum();

            Console.WriteLine($"Origin: GPU, Thread: {index}, sum: {sum}.");
        }
    }

    public readonly struct TestShader : IShader
    {
        public readonly WriteableStructuredBuffer<float> DestinationBuffer;

        public readonly Func<float, float> f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TestShader(WriteableStructuredBuffer<float> buffer, Func<float, float> f)
        {
            DestinationBuffer = buffer;
            this.f = f;
        }

        public void Accept(ShaderGeneratorContext context)
        {
            context.UnorderedAccessViews.Add(DestinationBuffer);
        }

        [ShaderMethod]
        [Shader("compute")]
        [NumThreads(32, 1, 1)]
        public void Execute(CSInput input)
        {
            DestinationBuffer[input.DispatchThreadId.X] = f(input.DispatchThreadId.X);
        }
    }
}
