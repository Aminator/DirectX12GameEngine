﻿using System.Numerics;
using DirectX12GameEngine.Rendering.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    [StaticShaderClass]
    public static class NormalStream
    {
        [ShaderResource] [NormalSemantic(0)] public static Vector3 Normal;

        [ShaderResource] [NormalSemantic(1)] public static Vector3 NormalWS;

        [ShaderResource] [TangentSemantic] public static Vector4 Tangent;

        [ShaderResource] public static Matrix4x4 TangentMatrix;

        [ShaderResource] public static Matrix4x4 TangentToWorld;
    }
}