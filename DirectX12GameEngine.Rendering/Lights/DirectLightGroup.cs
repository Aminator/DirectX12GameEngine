using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    [ConstantBufferResource]
    public abstract class DirectLightGroup
    {
        [ShaderResource] public int LightCount;

        /// <summary>
        /// Compute the light color/direction for the specified index within this group.
        /// </summary>
        [ShaderMethod]
        public void PrepareDirectLight(int lightIndex)
        {
            PrepareDirectLightCore(lightIndex);

            LightStream.NDotL = Math.Max(Vector3.Dot(NormalStream.NormalWS, LightStream.LightDirectionWS), 0.0001f);

            LightStream.LightColorNDotL = LightStream.LightColor * LightStream.NDotL;
            LightStream.LightSpecularColorNDotL = LightStream.LightColorNDotL;
        }

        [ShaderMethod]
        protected virtual void PrepareDirectLightCore(int lightIndex)
        {
        }
    }
}
