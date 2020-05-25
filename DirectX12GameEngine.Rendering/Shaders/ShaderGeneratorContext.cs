using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering
{
    public class ShaderGeneratorContext
    {
        public ShaderGeneratorContext(GraphicsDevice device, params Attribute[] entryPointAttributes) : this(device, new ShaderGeneratorSettings(entryPointAttributes))
        {
        }

        public ShaderGeneratorContext(GraphicsDevice device, ShaderGeneratorSettings settings)
        {
            GraphicsDevice = device;
            Settings = settings;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public IShader? Shader { get; private set; }

        public ShaderGeneratorSettings Settings { get; set; }

        public int ConstantBufferViewRegisterCount { get; set; }

        public int ShaderResourceViewRegisterCount { get; set; }

        public int UnorderedAccessViewRegisterCount { get; set; }

        public int SamplerRegisterCount { get; set; }

        public IList<RootParameter> RootParameters { get; } = new List<RootParameter>();

        public IList<ConstantBufferView> ConstantBufferViews { get; } = new List<ConstantBufferView>();

        public IList<ShaderResourceView> ShaderResourceViews { get; } = new List<ShaderResourceView>();

        public IList<UnorderedAccessView> UnorderedAccessViews { get; } = new List<UnorderedAccessView>();

        public IList<Sampler> Samplers { get; } = new List<Sampler>();

        public virtual void Visit(IShader shader)
        {
            Shader = shader;
            shader.Accept(this);
        }

        public virtual void Clear()
        {
            ConstantBufferViewRegisterCount = 0;
            ShaderResourceViewRegisterCount = 0;
            UnorderedAccessViewRegisterCount = 0;
            SamplerRegisterCount = 0;

            RootParameters.Clear();
            ConstantBufferViews.Clear();
            ShaderResourceViews.Clear();
            UnorderedAccessViews.Clear();
            Samplers.Clear();
        }

        public virtual async Task<PipelineState> CreateComputePipelineStateAsync()
        {
            CompiledShader compiledShader = await CreateShaderAsync();
            RootSignature rootSignature = CreateRootSignature();

            PipelineState pipelineState = new PipelineState(GraphicsDevice, rootSignature, compiledShader.Shaders["compute"]);

            return pipelineState;
        }

        public async Task<PipelineState> CreateGraphicsPipelineStateAsync(InputElementDescription[] inputElements)
        {
            CompiledShader compiledShader = await CreateShaderAsync();
            RootSignature rootSignature = CreateRootSignature();

            PipelineState pipelineState = new PipelineState(GraphicsDevice, rootSignature, inputElements,
                compiledShader.Shaders["vertex"],
                compiledShader.Shaders["pixel"],
                compiledShader.Shaders.ContainsKey("geometry") ? compiledShader.Shaders["geometry"] : null,
                compiledShader.Shaders.ContainsKey("hull") ? compiledShader.Shaders["hull"] : null,
                compiledShader.Shaders.ContainsKey("domain") ? compiledShader.Shaders["domain"] : null);

            return pipelineState;
        }

        public virtual Task<CompiledShader> CreateShaderAsync()
        {
            if (Shader is null) throw new InvalidOperationException();

            CompiledShader compiledShader = new CompiledShader();

            ShaderGenerator shaderGenerator = new ShaderGenerator(Shader, Settings);
            ShaderGeneratorResult result = shaderGenerator.GenerateShader();

            foreach (var entryPoint in result.EntryPoints)
            {
                compiledShader.Shaders[entryPoint.Key] = ShaderCompiler.Compile(GetShaderStage(entryPoint.Key), result.ShaderSource, entryPoint.Value);
            }

            return Task.FromResult(compiledShader);
        }

        public DescriptorSet? CreateShaderResourceViewDescriptorSet()
        {
            int shaderResourceCount = ConstantBufferViews.Count + ShaderResourceViews.Count + UnorderedAccessViews.Count;

            if (shaderResourceCount > 0)
            {
                DescriptorSet shaderResourceViewDescriptorSet = new DescriptorSet(GraphicsDevice, shaderResourceCount);
                shaderResourceViewDescriptorSet.AddResourceViews(ConstantBufferViews);
                shaderResourceViewDescriptorSet.AddResourceViews(ShaderResourceViews);
                shaderResourceViewDescriptorSet.AddResourceViews(UnorderedAccessViews);

                return shaderResourceViewDescriptorSet;
            }

            return null;
        }

        public DescriptorSet? CreateSamplerDescriptorSet()
        {
            int samplerCount = Samplers.Count;

            if (samplerCount > 0)
            {
                return new DescriptorSet(GraphicsDevice, Samplers);
            }

            return null;
        }

        public virtual RootSignature CreateRootSignature()
        {
            List<RootParameter> rootParameters = new List<RootParameter>(RootParameters);

            FillShaderResourceViewRootParameters(rootParameters);
            FillSamplerRootParameters(rootParameters);

            RootSignatureDescription rootSignatureDescription = new RootSignatureDescription(RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters.ToArray());

            return new RootSignature(GraphicsDevice, rootSignatureDescription);
        }

        private void FillShaderResourceViewRootParameters(IList<RootParameter> rootParameters)
        {
            List<DescriptorRange> shaderResourceViewRootDescriptorRanges = new List<DescriptorRange>();

            if (ConstantBufferViews.Count > 0)
            {
                shaderResourceViewRootDescriptorRanges.Add(new DescriptorRange(DescriptorRangeType.ConstantBufferView, ConstantBufferViews.Count, ConstantBufferViewRegisterCount));
            }

            if (ShaderResourceViews.Count > 0)
            {
                shaderResourceViewRootDescriptorRanges.Add(new DescriptorRange(DescriptorRangeType.ShaderResourceView, ShaderResourceViews.Count, ShaderResourceViewRegisterCount));
            }

            if (UnorderedAccessViews.Count > 0)
            {
                shaderResourceViewRootDescriptorRanges.Add(new DescriptorRange(DescriptorRangeType.UnorderedAccessView, UnorderedAccessViews.Count, UnorderedAccessViewRegisterCount));
            }

            if (shaderResourceViewRootDescriptorRanges.Count > 0)
            {
                rootParameters.Add(new RootParameter(new RootDescriptorTable(shaderResourceViewRootDescriptorRanges.ToArray()), ShaderVisibility.All));
            }
        }

        private void FillSamplerRootParameters(List<RootParameter> rootParameters)
        {
            if (Samplers.Count > 0)
            {
                rootParameters.Add(new RootParameter(new RootDescriptorTable(new DescriptorRange(DescriptorRangeType.Sampler, Samplers.Count, SamplerRegisterCount)), ShaderVisibility.All));
            }
        }

        public ShaderStage GetShaderStage(string shader) => shader switch
        {
            "vertex" => ShaderStage.VertexShader,
            "pixel" => ShaderStage.PixelShader,
            "geometry" => ShaderStage.GeometryShader,
            "hull" => ShaderStage.HullShader,
            "domain" => ShaderStage.DomainShader,
            "compute" => ShaderStage.ComputeShader,
            _ => ShaderStage.Library
        };
    }
}
