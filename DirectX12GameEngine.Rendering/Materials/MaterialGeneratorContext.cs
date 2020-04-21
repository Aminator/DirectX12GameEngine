using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Serialization;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class MaterialGeneratorContext : ShaderGeneratorContext
    {
        private readonly Stack<IMaterialDescriptor> materialDescriptorStack = new Stack<IMaterialDescriptor>();

        public MaterialGeneratorContext(GraphicsDevice device, Material material, IContentManager contentManager) : base(device)
        {
            Material = material;
            Content = contentManager;
        }

        public Material Material { get; }

        public IContentManager Content { get; }

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

            Clear();

            return materialPass;
        }

        public override async Task<CompiledShader> CreateShaderAsync()
        {
            if (MaterialDescriptor is null) throw new InvalidOperationException("The current material descriptor cannot be null when creating a pipeline state.");
            if (Shader is null) throw new InvalidOperationException();

            CompiledShader compiledShader = new CompiledShader();

            string shaderCachePath = Path.Combine("Log", "ShaderCache");
            string filePath = Path.Combine(shaderCachePath, $"Shader_{MaterialDescriptor.Id}");

            if (!await Content.ExistsAsync(filePath))
            {
                ShaderGenerator shaderGenerator = new ShaderGenerator(Shader, Settings);
                ShaderGeneratorResult result = shaderGenerator.GenerateShader();

                CompiledShaderAsset shaderAsset = new CompiledShaderAsset();

                foreach (var entryPoint in result.EntryPoints)
                {
                    compiledShader.Shaders[entryPoint.Key] = ShaderCompiler.Compile(GetShaderStage(entryPoint.Key), result.ShaderSource, entryPoint.Value);
                    shaderAsset.ShaderSources[entryPoint.Key] = Path.Combine(shaderCachePath, $"{entryPoint.Key}_{MaterialDescriptor.Id}.cso");

                    using Stream stream = await Content.FileProvider.OpenStreamAsync(shaderAsset.ShaderSources[entryPoint.Key], FileMode.Create, FileAccess.ReadWrite);
                    await stream.WriteAsync(compiledShader.Shaders[entryPoint.Key], 0, compiledShader.Shaders[entryPoint.Key].Length);
                }

                await Content.SaveAsync(filePath, shaderAsset);
            }
            else
            {
                compiledShader = await Content.LoadAsync<CompiledShader>(filePath);
            }

            return compiledShader;
        }
    }
}
