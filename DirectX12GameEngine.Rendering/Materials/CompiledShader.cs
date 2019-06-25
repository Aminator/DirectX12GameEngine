namespace DirectX12GameEngine.Rendering.Materials
{
    public class CompiledShader
    {
        public byte[]? ComputeShader { get; set; }

        public byte[]? VertexShader { get; set; }

        public byte[]? PixelShader { get; set; }

        public byte[]? HullShader { get; set; }

        public byte[]? DomainShader { get; set; }

        public byte[]? GeometryShader { get; set; }

        public byte[]? RayGenerationShader { get; set; }

        public byte[]? IntersectionShader { get; set; }

        public byte[]? AnyHitShader { get; set; }

        public byte[]? ClosestHitShader { get; set; }

        public byte[]? MissShader { get; set; }

        public byte[]? CallableShader { get; set; }
    }
}
