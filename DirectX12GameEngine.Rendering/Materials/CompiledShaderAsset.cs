using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class CompiledShaderAsset : Asset<CompiledShader>
    {
        public Dictionary<string, string> ShaderSources { get; } = new Dictionary<string, string>();

        public override async Task CreateAssetAsync(CompiledShader obj, IServiceProvider services)
        {
            ShaderContentManager shaderContentManager = services.GetRequiredService<ShaderContentManager>();

            foreach (var shaderSource in ShaderSources)
            {
                obj.Shaders[shaderSource.Key] = (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder!.GetFileAsync(shaderSource.Value))).ToArray();
            }
        }
    }
}
