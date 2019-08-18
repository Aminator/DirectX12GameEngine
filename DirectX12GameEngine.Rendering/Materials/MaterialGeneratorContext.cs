using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;
using Vortice.DirectX.Direct3D12;
using Vortice.DirectX.DXGI;
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

        public IList<GraphicsResource> ConstantBufferViews { get; } = new List<GraphicsResource>();

        public IList<GraphicsResource> ShaderResourceViews { get; } = new List<GraphicsResource>();

        public IList<GraphicsResource> UnorderedAccessViews { get; } = new List<GraphicsResource>();

        public IList<GraphicsResource> Samplers { get; } = new List<GraphicsResource>();

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

                compiledShader.VertexShader = result.VertexShader is null ? throw new Exception("Vertex shader must be present.") : ShaderCompiler.CompileShader(shaderSource, ShaderProfile.VertexShader, Shaders.ShaderModel.Model6_1, result.VertexShader);
                compiledShader.PixelShader = result.PixelShader is null ? throw new Exception("Pixel shader must be present.") : ShaderCompiler.CompileShader(shaderSource, ShaderProfile.PixelShader, Shaders.ShaderModel.Model6_1, result.PixelShader);
                compiledShader.HullShader = result.HullShader is null ? default : ShaderCompiler.CompileShader(shaderSource, ShaderProfile.HullShader, Shaders.ShaderModel.Model6_1, result.HullShader);
                compiledShader.DomainShader = result.DomainShader is null ? default : ShaderCompiler.CompileShader(shaderSource, ShaderProfile.DomainShader, Shaders.ShaderModel.Model6_1, result.DomainShader);
                compiledShader.GeometryShader = result.GeometryShader is null ? default : ShaderCompiler.CompileShader(shaderSource, ShaderProfile.GeometryShader, Shaders.ShaderModel.Model6_1, result.GeometryShader);

                CompiledShaderAsset shaderAsset = new CompiledShaderAsset
                {
                    VertexShaderSource = $"VertexShader_{MaterialDescriptor.MaterialId}.cso",
                    PixelShaderSource = $"PixelShader_{MaterialDescriptor.MaterialId}.cso",
                    HullShaderSource = result.HullShader is null ? null : $"HullShader_{MaterialDescriptor.MaterialId}.cso",
                    DomainShaderSource = result.DomainShader is null ? null : $"DomainShader_{MaterialDescriptor.MaterialId}.cso",
                    GeometryShaderSource = result.GeometryShader is null ? null : $"GeometryShader_{MaterialDescriptor.MaterialId}.cso",
                };

                await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.VertexShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.VertexShader);
                await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.PixelShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.PixelShader);
                if (shaderAsset.HullShaderSource != null) await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.HullShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.HullShader);
                if (shaderAsset.DomainShaderSource != null) await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.DomainShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.DomainShader);
                if (shaderAsset.GeometryShaderSource != null) await FileIO.WriteBytesAsync(await Content.RootFolder.CreateFileAsync(shaderAsset.GeometryShaderSource, CreationCollisionOption.ReplaceExisting), compiledShader.GeometryShader);

                await Content.SaveAsync(fileName, shaderAsset);
            }
            else
            {
                compiledShader = await Content.LoadAsync<CompiledShader>(fileName);
            }

            ID3D12RootSignature rootSignature = CreateRootSignature();

            return new PipelineState(GraphicsDevice, inputElements, rootSignature,
                compiledShader.VertexShader, compiledShader.PixelShader, compiledShader.HullShader, compiledShader.DomainShader, compiledShader.GeometryShader);
        }

        public ID3D12RootSignature CreateRootSignature()
        {
            int cbvShaderRegister = 0;

            List<RootParameter1> rootParameters = new List<RootParameter1>
            {
                new RootParameter1 { ParameterType = RootParameterType.Constant32Bits, Constants = new RootConstants(cbvShaderRegister++, 0, 1) },
                new RootParameter1 { DescriptorTable = new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)) },
                new RootParameter1 { DescriptorTable = new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)) },
                new RootParameter1 { DescriptorTable = new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)) },
                new RootParameter1 { DescriptorTable = new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvShaderRegister++)) }
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
                shaderResourceRootDescriptorRanges.Add(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, UnorderedAccessViews.Count, 1));
            }

            if (shaderResourceRootDescriptorRanges.Count > 0)
            {
                rootParameters.Add(new RootParameter1 { DescriptorTable = new RootDescriptorTable1(shaderResourceRootDescriptorRanges.ToArray()) });
            }

            if (Samplers.Count > 0)
            {
                rootParameters.Add(new RootParameter1 { DescriptorTable = new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.Sampler, Samplers.Count, 0)) });
            }

            StaticSamplerDescription[] staticSamplers = new StaticSamplerDescription[]
            {
                new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
            };

            RootSignatureDescription1 rootSignatureDescription = new RootSignatureDescription1(
                RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters.ToArray(), staticSamplers);

            return GraphicsDevice.CreateRootSignature(new VersionedRootSignatureDescription(rootSignatureDescription));
        }
    }
}
