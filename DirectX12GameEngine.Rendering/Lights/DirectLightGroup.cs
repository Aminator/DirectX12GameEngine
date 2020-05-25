using System;
using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Lights
{
    [ShaderContract]
    [ConstantBufferView]
    public abstract class DirectLightGroup
    {
        [ShaderMember]
        public int LightCount;

        /// <summary>
        /// Compute the light color for the specified index within this group.
        /// </summary>
        [ShaderMethod]
        public virtual Vector3 ComputeLightColor(int lightIndex)
        {
            return default;
        }

        /// <summary>
        /// Compute the light direction for the specified index within this group.
        /// </summary>
        [ShaderMethod]
        public virtual Vector3 ComputeLightDirection(int lightIndex)
        {
            return default;
        }
    }
}
