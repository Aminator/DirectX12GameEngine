using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class CompiledShaderAsset : Asset<CompiledShader>
    {
        public Dictionary<string, string> ShaderSources { get; } = new Dictionary<string, string>();

        public override async Task CreateAssetAsync(CompiledShader obj, IServiceProvider services)
        {
            IContentManager contentManager = services.GetRequiredService<IContentManager>();

            foreach (var shaderSource in ShaderSources)
            {
                using Stream stream = await contentManager.FileProvider.OpenStreamAsync(shaderSource.Value, FileMode.Open, FileAccess.Read);
                using MemoryStream memoryStream = new MemoryStream();

                await stream.CopyToAsync(memoryStream);

                obj.Shaders[shaderSource.Key] = memoryStream.ToArray();
            }
        }
    }
}
