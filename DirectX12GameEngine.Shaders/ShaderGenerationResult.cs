using System;
using System.Reflection;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGenerationResult
    {
        public string? ShaderSource { get; set; }

        public string? ComputeShader { get; set; }

        public string? VertexShader { get; set; }

        public string? PixelShader { get; set; }

        public string? HullShader { get; set; }

        public string? DomainShader { get; set; }

        public string? GeometryShader { get; set; }

        public string? RayGenerationShader { get; set; }

        public string? IntersectionShader { get; set; }

        public string? AnyHitShader { get; set; }

        public string? ClosestHitShader { get; set; }

        public string? MissShader { get; set; }

        public string? CallableShader { get; set; }

        internal void SetShader(string name, MethodInfo methodInfo)
        {
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                if (name == propertyInfo.Name.Replace("Shader", "").ToLower())
                {
                    propertyInfo.SetValue(this, methodInfo.Name);
                    return;
                }
            }

            throw new NotSupportedException("Attribute 'Shader' must have one of these values: compute, vertex, pixel, hull, domain, geometry, raygeneration, intersection, anyhit, closesthit, miss, callable.");
        }
    }
}
