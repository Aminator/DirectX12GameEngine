using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DirectX12GameEngine.Core
{
    public static class VertexHelper
    {
        public static Span<Vector4> GenerateTangents(Span<byte> positionBuffer, Span<byte> texCoordBuffer, Span<byte> indexBuffer = default, bool is32bitIndex = false)
        {
            Span<ushort> indexBuffer16 = !indexBuffer.IsEmpty && !is32bitIndex ? MemoryMarshal.Cast<byte, ushort>(indexBuffer) : default;
            Span<int> indexBuffer32 = !indexBuffer.IsEmpty && is32bitIndex ? MemoryMarshal.Cast<byte, int>(indexBuffer) : default;

            Span<Vector3> posBuffer = MemoryMarshal.Cast<byte, Vector3>(positionBuffer);
            Span<Vector2> uvBuffer = MemoryMarshal.Cast<byte, Vector2>(texCoordBuffer);

            Span<Vector4> tangentBuffer = new Vector4[posBuffer.Length];

            int indexCount = indexBuffer.IsEmpty
                ? positionBuffer.Length / Unsafe.SizeOf<Vector3>()
                : indexBuffer32.IsEmpty ? indexBuffer16.Length : indexBuffer32.Length;

            for (int i = 0; i < indexCount; i += 3)
            {
                int index1 = i + 0;
                int index2 = i + 1;
                int index3 = i + 2;

                if (!indexBuffer16.IsEmpty)
                {
                    index1 = indexBuffer16[index1];
                    index2 = indexBuffer16[index2];
                    index3 = indexBuffer16[index3];
                }
                else if (!indexBuffer32.IsEmpty)
                {
                    index1 = indexBuffer32[index1];
                    index2 = indexBuffer32[index2];
                    index3 = indexBuffer32[index3];
                }

                Vector3 position1 = posBuffer[index1];
                Vector3 position2 = posBuffer[index2];
                Vector3 position3 = posBuffer[index3];

                Vector2 uv1 = uvBuffer[index1];
                Vector2 uv2 = uvBuffer[index2];
                Vector2 uv3 = uvBuffer[index3];

                Vector3 edge1 = position2 - position1;
                Vector3 edge2 = position3 - position1;

                Vector2 uvEdge1 = uv2 - uv1;
                Vector2 uvEdge2 = uv3 - uv1;

                float dR = uvEdge1.X * uvEdge2.Y - uvEdge2.X * uvEdge1.Y;

                if (Math.Abs(dR) < 1e-6f)
                {
                    dR = 1.0f;
                }

                float r = 1.0f / dR;
                Vector3 t = (uvEdge2.Y * edge1 - uvEdge1.Y * edge2) * r;

                tangentBuffer[index1] += new Vector4(t, 0.0f);
                tangentBuffer[index2] += new Vector4(t, 0.0f);
                tangentBuffer[index3] += new Vector4(t, 0.0f);
            }

            for (int i = 0; i < tangentBuffer.Length; i++)
            {
                tangentBuffer[i].W = 1.0f;
            }

            return tangentBuffer;
        }
    }
}
