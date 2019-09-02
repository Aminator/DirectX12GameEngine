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
                ShaderGenerationResult result = shaderGenerator.GenerateShader();

                string shaderSource = result.ShaderSource;

                compiledShader.VertexShader = !result.EntryPoints.ContainsKey("vertex") ? throw new Exception("Vertex shader must be present.") : ShaderCompiler.Compile(DxcShaderStage.VertexShader, shaderSource, result.EntryPoints["vertex"]);
                compiledShader.PixelShader = !result.EntryPoints.ContainsKey("pixel") ? throw new Exception("Pixel shader must be present.") : ShaderCompiler.Compile(DxcShaderStage.PixelShader, shaderSource, result.EntryPoints["pixel"]);
                compiledShader.GeometryShader = !result.EntryPoints.ContainsKey("geometry") ? default : ShaderCompiler.Compile(DxcShaderStage.GeometryShader, shaderSource, result.EntryPoints["geometry"]);
                compiledShader.HullShader = !result.EntryPoints.ContainsKey("hull") ? default : ShaderCompiler.Compile(DxcShaderStage.HullShader, shaderSource, result.EntryPoints["hull"]);
                compiledShader.DomainShader = !result.EntryPoints.ContainsKey("domain") ? default : ShaderCompiler.Compile(DxcShaderStage.DomainShader, shaderSource, result.EntryPoints["domain"]);

                CompiledShaderAsset shaderAsset = new CompiledShaderAsset
                {
                    VertexShaderSource = $"VertexShader_{MaterialDescriptor.MaterialId}.cso",
                    PixelShaderSource = $"PixelShader_{MaterialDescriptor.MaterialId}.cso",
                    GeometryShaderSource = !result.EntryPoints.ContainsKey("geometry") ? null : $"GeometryShader_{MaterialDescriptor.MaterialId}.cso",
                    HullShaderSource = !result.EntryPoints.ContainsKey("hull") ? null : $"HullShader_{MaterialDescriptor.MaterialId}.cso",
                    DomainShaderSource = !result.EntryPoints.ContainsKey("domain") ? null : $"DomainShader_{MaterialDescriptor.MaterialId}.cso",
                };

                await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.VertexShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.VertexShader);
                await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.PixelShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.PixelShader);
                if (shaderAsset.GeometryShaderSource != null) await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.GeometryShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.GeometryShader);
                if (shaderAsset.HullShaderSource != null) await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.HullShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.HullShader);
                if (shaderAsset.DomainShaderSource != null) await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.DomainShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.DomainShader);

                await Content.SaveAsync(fileName, shaderAsset);
            }
            else
            {
                compiledShader = await Content.LoadAsync<CompiledShader>(fileName);
            }

            ID3D12RootSignature rootSignature = CreateRootSignature();

            return new PipelineState(GraphicsDevice, inputElements, rootSignature,
                compiledShader.VertexShader, compiledShader.PixelShader, compiledShader.GeometryShader, compiledShader.HullShader, compiledShader.DomainShader);
        }

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
