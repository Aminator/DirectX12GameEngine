using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class NormalStream
    {
        [ShaderMember] [NormalSemantic(0)] public static Vector3 Normal;

        [ShaderMember] [NormalSemantic(1)] public static Vector3 NormalWS;

        [ShaderMember] [TangentSemantic] public static Vector4 Tangent;

        [ShaderMember] public static Matrix4x4 TangentMatrix;

        [ShaderMember] public static Matrix4x4 TangentToWorld;
    }
}
