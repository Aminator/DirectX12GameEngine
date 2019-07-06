using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using Windows.Storage;
using PipelineState = DirectX12GameEngine.Graphics.PipelineState;

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

        public IList<Graphics.Buffer> ConstantBuffers { get; } = new List<Graphics.Buffer>();

        public IList<GraphicsResource> Samplers { get; } = new List<GraphicsResource>();

        public IList<Texture> Textures { get; } = new List<Texture>();

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

            Textures.Clear();

            return materialPass;
        }

        public async Task<PipelineState> CreateGraphicsPipelineStateAsync()
        {
            if (MaterialDescriptor is null) throw new InvalidOperationException("The current material descriptor cannot be null when creating a pipeline state.");

            InputElement[] inputElements = new[]
            {
                new InputElement("Position", 0, Format.R32G32B32_Float, 0),
                new InputElement("Normal", 0, Format.R32G32B32_Float, 1),
                new InputElement("Tangent", 0, Format.R32G32B32A32_Float, 2),
                new InputElement("TexCoord", 0, Format.R32G32_Float, 3)
            };

            CompiledShader compiledShader = new CompiledShader(); 

            string fileName = $"Shader_{MaterialDescriptor.MaterialId}";

            if (!await Content.ExistsAsync(fileName))
            {
                ShaderGenerator shaderGenerator = new ShaderGenerator(MaterialDescriptor.Attributes);
                ShaderGenerationResult result = await Task.Run(() => shaderGenerator.GenerateShader());

                string shaderSource = result.ShaderSource ?? throw new Exception();

                compiledShader.VertexShader = result.VertexShader is null ? throw new Exception("Vertex shader must be present.") : ShaderCompiler.CompileShader(shaderSource, SharpDX.D3DCompiler.ShaderVersion.VertexShader, result.VertexShader);
                compiledShader.PixelShader = result.PixelShader is null ? throw new Exception("Pixel shader must be present.") : ShaderCompiler.CompileShader(shaderSource, SharpDX.D3DCompiler.ShaderVersion.PixelShader, result.PixelShader);
                compiledShader.HullShader = result.HullShader is null ? default : ShaderCompiler.CompileShader(shaderSource, SharpDX.D3DCompiler.ShaderVersion.HullShader, result.HullShader);
                compiledShader.DomainShader = result.DomainShader is null ? default : ShaderCompiler.CompileShader(shaderSource, SharpDX.D3DCompiler.ShaderVersion.DomainShader, result.DomainShader);
                compiledShader.GeometryShader = result.GeometryShader is null ? default : ShaderCompiler.CompileShader(shaderSource, SharpDX.D3DCompiler.ShaderVersion.GeometryShader, result.GeometryShader);

                CompiledShaderAsset shaderAsset = new CompiledShaderAsset(Content)
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

            RootSignature rootSignature = CreateRootSignature();

            return new PipelineState(GraphicsDevice, inputElements, rootSignature,
                compiledShader.VertexShader, compiledShader.PixelShader, compiledShader.HullShader, compiledShader.DomainShader, compiledShader.GeometryShader);
        }

        public RootSignature CreateRootSignature()
        {
            List<RootParameter> rootParameters = new List<RootParameter>
            {
                new RootParameter(ShaderVisibility.All,
                    new RootConstants(0, 0, 1)),
                new RootParameter(ShaderVisibility.All,
                    new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 1)),
                new RootParameter(ShaderVisibility.All,
                    new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 2)),
                new RootParameter(ShaderVisibility.All,
                    new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 3)),
                new RootParameter(ShaderVisibility.All,
                    new DescriptorRange(DescriptorRangeType.ConstantBufferView, 1, 4))
            };

            if (ConstantBuffers.Count > 0)
            {
                rootParameters.Add(new RootParameter(ShaderVisibility.All,
                    new DescriptorRange(DescriptorRangeType.ConstantBufferView, ConstantBuffers.Count, 5)));
            }

            if (Samplers.Count > 0)
            {
                rootParameters.Add(new RootParameter(ShaderVisibility.All,
                    new DescriptorRange(DescriptorRangeType.Sampler, Samplers.Count, 1)));
            }

            if (Textures.Count > 0)
            {
                rootParameters.Add(new RootParameter(ShaderVisibility.All,
                    new DescriptorRange(DescriptorRangeType.ShaderResourceView, Textures.Count, 0)));
            }

            StaticSamplerDescription[] staticSamplers = new StaticSamplerDescription[]
            {
                new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
            };

            RootSignatureDescription rootSignatureDescription = new RootSignatureDescription(
                RootSignatureFlags.AllowInputAssemblerInputLayout, rootParameters.ToArray(), staticSamplers);

            return GraphicsDevice.CreateRootSignature(rootSignatureDescription);
        }
    }
}
