using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Shaders;

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
                    GraphicsBuffer<float> buffer = GraphicsBuffer.Create<float>(device, sumCount, ResourceFlags.AllowUnorderedAccess);
                    TestShader shader = new TestShader(buffer, Sigmoid);

                    ShaderGeneratorContext context = new ShaderGeneratorContext(device);
                    context.Visit(shader);

                    ShaderGeneratorResult result = new ShaderGenerator(shader).GenerateShader();

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

                    float sum = buffer.GetData().Sum();

                    Console.WriteLine($"Thread: {index}, sum: {sum}.");
                }));
            }

            Console.WriteLine("Awaiting tasks...");
            Console.WriteLine($"Task count: {tasks.Count}");

            await Task.WhenAll(tasks);

            Console.WriteLine("DONE!");
        }
    }

    public readonly struct TestShader : IComputeShader
    {
        [UnorderedAccessView(typeof(RWStructuredBufferResource<float>))]
        public readonly GraphicsBuffer<float> DestinationBuffer;

        public readonly Func<float, float> f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TestShader(GraphicsBuffer<float> buffer, Func<float, float> f)
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
