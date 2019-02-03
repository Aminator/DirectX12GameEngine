namespace DirectX12GameEngine
{
    public abstract class ShaderResource
    {
    }

    [SamplerResource]
    public class SamplerResource : ShaderResource
    {
    }

    [SamplerComparisonResource]
    public class SamplerComparisonResource : ShaderResource
    {
    }

    [Texture2DResource]
    public class Texture2DResource : ShaderResource
    {
    }

    [Texture2DArrayResource]
    public class Texture2DArrayResource : ShaderResource
    {
    }

    [TextureCubeResource]
    public class TextureCubeResource : ShaderResource
    {
    }
}
