using System;
using System.Reflection;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGenerationResult
    {
        public string? ShaderSource { get; set; }

        public MethodInfo? ComputeShader { get; set; }

        public MethodInfo? VertexShader { get; set; }

        public MethodInfo? PixelShader { get; set; }

        public MethodInfo? HullShader { get; set; }

        public MethodInfo? DomainShader { get; set; }

        public MethodInfo? GeometryShader { get; set; }

        public MethodInfo? RayGenerationShader { get; set; }

        public MethodInfo? IntersectionShader { get; set; }

        public MethodInfo? AnyHitShader { get; set; }

        public MethodInfo? ClosestHitShader { get; set; }

        public MethodInfo? MissShader { get; set; }

        public MethodInfo? CallableShader { get; set; }

        internal void SetShader(string name, MethodInfo methodInfo)
        {
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                if (name == propertyInfo.Name.Replace("Shader", "").ToLower())
                {
                    propertyInfo.SetValue(this, methodInfo);
                    return;
                }
            }

            throw new NotSupportedException("Attribute 'Shader' must have one of these values: compute, vertex, pixel, hull, domain, geometry, raygeneration, intersection, anyhit, closesthit, miss, callable.");
        }
    }
}
