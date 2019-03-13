using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class Global
    {
        [ShaderResource] public static float ElapsedTime;

        [ShaderResource] public static float TotalTime;
    }

    public struct GlobalBuffer
    {
        [ShaderResource] public float ElapsedTime { get; set; }
        [ShaderResource] public float TotalTime { get; set; }
    }
}
