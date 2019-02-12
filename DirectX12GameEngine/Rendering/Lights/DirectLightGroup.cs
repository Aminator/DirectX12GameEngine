using System;
using System.Numerics;
using DirectX12GameEngine.Rendering.Core;
using DirectX12GameEngine.Rendering.Shaders;

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

            // TODO: See why the System namespace in Math.Max is not present in UWP projects.
#if NETCOREAPP
            LightStream.NDotL = Math.Max(Vector3.Dot(NormalStream.Normal, LightStream.LightDirection), 0.0001f);
#else
            LightStream.NDotL = Vector3.Dot(NormalStream.Normal, LightStream.LightDirection);
            LightStream.NDotL = LightStream.NDotL > 0.0001f ? LightStream.NDotL : 0.0001f;
#endif

            LightStream.LightColorNDotL = LightStream.LightColor * LightStream.NDotL;
            LightStream.LightSpecularColorNDotL = LightStream.LightColorNDotL;
        }

        [ShaderMethod]
        protected virtual void PrepareDirectLightCore(int lightIndex)
        {
        }
    }
}
