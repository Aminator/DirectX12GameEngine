using System.Numerics;
using DirectX12GameEngine.Shaders;

namespace DirectX12GameEngine.Rendering.Core
{
    public static class NormalUpdate
    {
        [ShaderMethod]
        public static void UpdateNormalFromTangentSpace(Vector3 normalInTangetSpace)
        {
            Vector3 normal = Vector3.Normalize(NormalStream.Normal);

            Vector4 meshTangent = NormalStream.Tangent;
            Vector3 tangent = Vector3.Normalize(new Vector3(meshTangent.X, meshTangent.Y, meshTangent.Z));

            Vector3 bitangent = meshTangent.W * Vector3.Cross(normal, tangent);

            NormalStream.TangentMatrix = new Matrix4x4(
                tangent.X, tangent.Y, tangent.Z, 0.0f,
                bitangent.X, bitangent.Y, bitangent.Z, 0.0f,
                normal.X, normal.Y, normal.Z, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);

            NormalStream.TangentToWorld = Matrix4x4.Multiply(NormalStream.TangentMatrix, Transformation.WorldMatrix);
            NormalStream.NormalWS = Vector3.Normalize(Vector3.TransformNormal(normalInTangetSpace, NormalStream.TangentToWorld));
        }
    }
}
