using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class Global
    {
        [ShaderMember] public static float ElapsedTime;

        [ShaderMember] public static float TotalTime;
    }

    public struct GlobalBuffer
    {
        [ShaderMember] public float ElapsedTime { get; set; }

        [ShaderMember] public float TotalTime { get; set; }
    }
}
