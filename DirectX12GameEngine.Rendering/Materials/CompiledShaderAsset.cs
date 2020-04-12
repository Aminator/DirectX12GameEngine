using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class CompiledShaderAsset : Asset
    {
        public Dictionary<string, string> ShaderSources { get; } = new Dictionary<string, string>();

        public override async Task<object> CreateAssetAsync(IServiceProvider services)
        {
            IContentManager contentManager = services.GetRequiredService<IContentManager>();

            CompiledShader compiledShader = new CompiledShader();

            foreach (var shaderSource in ShaderSources)
            {
                using Stream stream = await contentManager.FileProvider.OpenStreamAsync(shaderSource.Value, FileMode.Open, FileAccess.Read);
                using MemoryStream memoryStream = new MemoryStream();

                await stream.CopyToAsync(memoryStream);
                compiledShader.Shaders[shaderSource.Key] = memoryStream.ToArray();
            }

            return compiledShader;
        }
    }
}
