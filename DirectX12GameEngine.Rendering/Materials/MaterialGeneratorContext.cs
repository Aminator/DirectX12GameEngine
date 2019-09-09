using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Dxc;
using Windows.Storage;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialGeneratorContext
    {
        private readonly Stack<IMaterialDescriptor> materialDescriptorStack = new Stack<IMaterialDescriptor>();

        public MaterialGeneratorContext(GraphicsDevice device, Material material, ShaderContentManager contentManager)
        {
            GraphicsDevice = device;
            Material = material;
            Content = contentManager;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public Material Material { get; }

        public ShaderContentManager Content { get; }

        public IList<GraphicsBuffer> ConstantBufferViews { get; } = new List<GraphicsBuffer>();

        public IList<GraphicsResource> ShaderResourceViews { get; } = new List<GraphicsResource>();

        public IList<GraphicsResource> UnorderedAccessViews { get; } = new List<GraphicsResource>();

        public IList<SamplerState> Samplers { get; } = new List<SamplerState>();

        public IMaterialDescriptor? MaterialDescriptor => materialDescriptorStack.Count > 0 ? materialDescriptorStack.Peek() : null;

        public MaterialPass? MaterialPass { get; private set; }

        public int PassCount { get; private set; } = 1;

        public int PassIndex { get; private set; }

        public void PushMaterialDescriptor(IMaterialDescriptor descriptor)
        {
            materialDescriptorStack.Push(descriptor);
        }

        public IMaterialDescriptor? PopMaterialDescriptor()
        {
            return materialDescriptorStack.Count > 0 ? materialDescriptorStack.Pop() : null;
        }

        public MaterialPass PushPass()
        {
            MaterialPass materialPass = new MaterialPass { PassIndex = PassIndex };
            Material.Passes.Add(materialPass);

            MaterialPass = materialPass;

            return materialPass;
        }

        public MaterialPass? PopPass()
        {
            PassIndex++;

            MaterialPass? materialPass = MaterialPass;
            MaterialPass = null;

            ConstantBufferViews.Clear();
            ShaderResourceViews.Clear();
            UnorderedAccessViews.Clear();

            return materialPass;
        }

        public async Task<PipelineState> CreateGraphicsPipelineStateAsync()
        {
            if (MaterialDescriptor is null) throw new InvalidOperationException("The current material descriptor cannot be null when creating a pipeline state.");

            InputElementDescription[] inputElements = new[]
            {
                new InputElementDescription("Position", 0, (Format)PixelFormat.R32G32B32_Float, 0),
                new InputElementDescription("Normal", 0, (Format)PixelFormat.R32G32B32_Float, 1),
                new InputElementDescription("Tangent", 0, (Format)PixelFormat.R32G32B32A32_Float, 2),
                new InputElementDescription("TexCoord", 0, (Format)PixelFormat.R32G32_Float, 3)
            };

            CompiledShader compiledShader = new CompiledShader();

            string fileName = $"Shader_{MaterialDescriptor.MaterialId}";

            if (!await Content.ExistsAsync(fileName))
            {
                ShaderGenerator shaderGenerator = new ShaderGenerator(MaterialDescriptor.Attributes);
                ShaderGeneratorResult result = shaderGenerator.GenerateShader();

                CompiledShaderAsset shaderAsset = new CompiledShaderAsset();

                foreach (var entryPoint in result.EntryPoints)
                {
                    compiledShader.Shaders[entryPoint.Key] = ShaderCompiler.Compile(GetShaderStage(entryPoint.Key), result.ShaderSource, entryPoint.Value);
                    shaderAsset.ShaderSources[entryPoint.Key] = $"{entryPoint.Key}_{MaterialDescriptor.MaterialId}.cso";
                    await FileIO.WriteBytesAsync(await Content.RootFolder!.CreateFileAsync(shaderAsset.ShaderSources[entryPoint.Key], CreationCollisionOption.ReplaceExisting), compiledShader.Shaders[entryPoint.Key]);
                }

                await Content.SaveAsync(fileName, shaderAsset);
            }
            else
            {
                compiledShader = await Content.LoadAsync<CompiledShader>(fileName);
            }

            ID3D12RootSignature rootSignature = CreateRootSignature();

            return new PipelineState(GraphicsDevice, inputElements, rootSignature,
                compiledShader.Shaders["vertex"],
                compiledShader.Shaders["pixel"],
                compiledShader.Shaders.ContainsKey("geometry") ? compiledShader.Shaders["geometry"] : null,
                compiledShader.Shaders.ContainsKey("hull") ? compiledShader.Shaders["hull"] : null,
                compiledShader.Shaders.ContainsKey("domain") ? compiledShader.Shaders["domain"] : null);
        }

        private DxcShaderStage GetShaderStage(string shader) => shader switch
        {
            "vertex" => DxcShaderStage.VertexShader,
            "pixel" => DxcShaderStage.PixelShader,
            "geometry" => DxcShaderStage.GeometryShader,
            "hull" => DxcShaderStage.HullShader,
            "domain" => DxcShaderStage.DomainShader,
            "compute" => DxcShaderStage.ComputeShader,
            _ => DxcShaderStage.Library
        };

        public ID3D12RootSignature CreateRootSignature()
        {
            int cbvShaderRegister = 0;

            List<RootParameter1> rootParameters = new List<RootParameter1>
            {
                new RootParameter1(new RootConstants(cbvShaderRegister++, 0, 1), ShaderVisibility.All),
                new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)), ShaderVisibility.All),
                new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)), ShaderVisibility.All),
                new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)), ShaderVisibility.All),
                new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)), ShaderVisibility.All),
                new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.Sampler, 1, 0)), ShaderVisibility.All)
            };

            List<DescriptorRange1> shaderResourceRootDescriptorRanges = new List<DescriptorRange1>();

            if (ConstantBufferViews.Count > 0)
            {
                shaderResourceRootDescriptorRanges.Add(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, ConstantBufferViews.Count, cbvShaderRegister));
            }

            if (ShaderResourceViews.Count > 0)
            {
                shaderResourceRootDescriptorRanges.Add(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, ShaderResourceViews.Count, 0));
            }

            if (UnorderedAccessViews.Count > 0)
            {
                shaderResourceRootDescriptorRanges.Add(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, UnorderedAccessViews.Count, 0));
            }

            if (shaderResourceRootDescriptorRanges.Count > 0)
            {
                rootParameters.Add(new RootParameter1(new RootDescriptorTable1(shaderResourceRootDescriptorRanges.ToArray()), ShaderVisibility.All));
            }

            if (Samplers.Count > 0)
            {
                rootParameters.Add(new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.Sampler, Samplers.Count, 1)), ShaderVisibility.All));
            }

            RootSignatureDescription1 rootSignatureDescription = new RootSignatureDescription1(
                RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters.ToArray());

            return GraphicsDevice.CreateRootSignature(new VersionedRootSignatureDescription(rootSignatureDescription));
        }
    }
}
