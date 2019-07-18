using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DirectX12GameEngine.Core.Assets;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;

namespace DirectX12GameEngine.Rendering.Materials
{
    public class CompiledShaderAsset : Asset<CompiledShader>
    {
        public string? ComputeShaderSource { get; set; }

        public string? VertexShaderSource { get; set; }

        public string? PixelShaderSource { get; set; }

        public string? HullShaderSource { get; set; }

        public string? DomainShaderSource { get; set; }

        public string? GeometryShaderSource { get; set; }

        public string? RayGenerationShaderSource { get; set; }

        public string? IntersectionShaderSource { get; set; }

        public string? AnyHitShaderSource { get; set; }

        public string? ClosestHitShaderSource { get; set; }

        public string? MissShaderSource { get; set; }

        public string? CallableShaderSource { get; set; }

        public override async Task CreateAssetAsync(CompiledShader obj, IServiceProvider services)
        {
            ShaderContentManager shaderContentManager = services.GetRequiredService<ShaderContentManager>();

            obj.ComputeShader = ComputeShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(ComputeShaderSource))).ToArray();
            obj.VertexShader = VertexShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(VertexShaderSource))).ToArray();
            obj.PixelShader = PixelShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(PixelShaderSource))).ToArray();
            obj.HullShader = HullShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(HullShaderSource))).ToArray();
            obj.DomainShader = DomainShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(DomainShaderSource))).ToArray();
            obj.GeometryShader = GeometryShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(GeometryShaderSource))).ToArray();
            obj.RayGenerationShader = RayGenerationShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(RayGenerationShaderSource))).ToArray();
            obj.IntersectionShader = IntersectionShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(IntersectionShaderSource))).ToArray();
            obj.AnyHitShader = AnyHitShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(AnyHitShaderSource))).ToArray();
            obj.ClosestHitShader = ClosestHitShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(ClosestHitShaderSource))).ToArray();
            obj.MissShader = MissShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(MissShaderSource))).ToArray();
            obj.CallableShader = CallableShaderSource is null ? null : (await FileIO.ReadBufferAsync(await shaderContentManager.RootFolder.GetFileAsync(CallableShaderSource))).ToArray();
        }
    }
}
