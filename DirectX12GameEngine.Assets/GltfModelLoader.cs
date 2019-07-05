using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering.Materials;
using GltfLoader;
using GltfLoader.Schema;
using SharpDX.Direct3D12;

using Texture = DirectX12GameEngine.Graphics.Texture;

namespace DirectX12GameEngine.Rendering
{
    internal class GltfModelLoader
    {
        public GltfModelLoader(GraphicsDevice device)
        {
            GraphicsDevice = device;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public static Task<Gltf> LoadGltfModelAsync(string filePath)
        {
            return Task.Run(() => Interface.LoadModel(filePath));
        }

        public static async Task<Gltf> LoadGltfModelAsync(Stream stream)
        {
            Gltf gltf = await Task.Run(() => Interface.LoadModel(stream));
            return gltf;
        }

        public async Task<MaterialAttributes> LoadMaterialAsync(string filePath, int materialIndex)
        {
            Gltf gltf = await LoadGltfModelAsync(filePath);
            IList<byte[]> buffers = GetGltfModelBuffers(gltf, filePath);

            return await GetMaterialAsync(gltf, buffers, materialIndex);
        }

        public async Task<IList<MaterialAttributes>> LoadMaterialsAsync(string filePath)
        {
            Gltf gltf = await LoadGltfModelAsync(filePath);
            IList<byte[]> buffers = GetGltfModelBuffers(gltf, filePath);

            return await GetMaterialsAsync(gltf, buffers);
        }

        public async Task<Mesh> LoadMeshAsync(string filePath, int meshIndex)
        {
            Gltf gltf = await LoadGltfModelAsync(filePath);
            IList<byte[]> buffers = GetGltfModelBuffers(gltf, filePath);

            return await GetMeshAsync(gltf, buffers, meshIndex);
        }

        public async Task<IList<Mesh>> LoadMeshesAsync(string filePath)
        {
            Gltf gltf = await LoadGltfModelAsync(filePath);
            IList<byte[]> buffers = GetGltfModelBuffers(gltf, filePath);

            return await GetMeshesAsync(gltf, buffers);
        }

        public async Task<MaterialAttributes> LoadMaterialAsync(Stream stream, int materialIndex)
        {
            var (gltf, buffers) = await GetGltfModelAndBuffersAsync(stream);

            return await GetMaterialAsync(gltf, buffers, materialIndex);
        }

        public async Task<IList<MaterialAttributes>> LoadMaterialsAsync(Stream stream)
        {
            var (gltf, buffers) = await GetGltfModelAndBuffersAsync(stream);

            return await GetMaterialsAsync(gltf, buffers);
        }

        public async Task<Mesh> LoadMeshAsync(Stream stream, int meshIndex)
        {
            var (gltf, buffers) = await GetGltfModelAndBuffersAsync(stream);

            return await GetMeshAsync(gltf, buffers, meshIndex);
        }

        public async Task<IList<Mesh>> LoadMeshesAsync(Stream stream)
        {
            var (gltf, buffers) = await GetGltfModelAndBuffersAsync(stream);

            return await GetMeshesAsync(gltf, buffers);
        }

        private static int GetCountOfAccessorType(Accessor.TypeEnum type) => type switch
        {
            Accessor.TypeEnum.Scalar => 1,
            Accessor.TypeEnum.Vec2 => 2,
            Accessor.TypeEnum.Vec3 => 3,
            Accessor.TypeEnum.Vec4 => 4,
            Accessor.TypeEnum.Mat2 => 4,
            Accessor.TypeEnum.Mat3 => 9,
            Accessor.TypeEnum.Mat4 => 16,
            _ => throw new NotSupportedException("This type is not supported.")
        };

        private static async Task<(Gltf, IList<byte[]>)> GetGltfModelAndBuffersAsync(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            using MemoryStream memoryStream1 = new MemoryStream(buffer);
            using MemoryStream memoryStream2 = new MemoryStream(buffer);

            Gltf gltf = await LoadGltfModelAsync(memoryStream1);
            IList<byte[]> buffers = GetGltfModelBuffers(gltf, memoryStream2);

            return (gltf, buffers);
        }

        private static IList<byte[]> GetGltfModelBuffers(Gltf gltf, string filePath)
        {
            byte[][] buffers = new byte[gltf.Buffers.Length][];

            for (int i = 0; i < gltf.Buffers.Length; i++)
            {
                buffers[i] = gltf.LoadBinaryBuffer(i, Path.Combine(Path.GetDirectoryName(filePath), gltf.Buffers[i].Uri ?? Path.GetFileName(filePath)));
            }

            return buffers;
        }

        private static IList<byte[]> GetGltfModelBuffers(Gltf gltf, Stream stream)
        {
            byte[][] buffers = new byte[gltf.Buffers.Length][];

            for (int i = 0; i < gltf.Buffers.Length; i++)
            {
                buffers[i] = Interface.LoadBinaryBuffer(stream);
            }

            return buffers;
        }

        private async Task<IList<MaterialAttributes>> GetMaterialsAsync(Gltf gltf, IList<byte[]> buffers)
        {
            List<MaterialAttributes> materials = new List<MaterialAttributes>(gltf.Materials.Length);

            for (int i = 0; i < gltf.Materials.Length; i++)
            {
                MaterialAttributes material = await GetMaterialAsync(gltf, buffers, i);
                materials.Add(material);
            }

            return materials;
        }

        private async Task<MaterialAttributes> GetMaterialAsync(Gltf gltf, IList<byte[]> buffers, int materialIndex)
        {
            GltfLoader.Schema.Material material = gltf.Materials[materialIndex];

            int? diffuseTextureIndex = material.PbrMetallicRoughness.BaseColorTexture?.Index;
            int? metallicRoughnessTextureIndex = material.PbrMetallicRoughness.MetallicRoughnessTexture?.Index;
            int? normalTextureIndex = material.NormalTexture?.Index;

            if (!diffuseTextureIndex.HasValue)
            {
                if (material.Extensions?.FirstOrDefault().Value is Newtonsoft.Json.Linq.JObject jObject && jObject.TryGetValue("diffuseTexture", out Newtonsoft.Json.Linq.JToken token))
                {
                    if (token.FirstOrDefault(t => (t as Newtonsoft.Json.Linq.JProperty)?.Name == "index") is Newtonsoft.Json.Linq.JProperty indexToken)
                    {
                        diffuseTextureIndex = (int)indexToken.Value;
                    }
                }
            }

            int? specularGlossinessTextureIndex = null;

            if (!metallicRoughnessTextureIndex.HasValue)
            {
                if (material.Extensions?.FirstOrDefault().Value is Newtonsoft.Json.Linq.JObject jObject && jObject.TryGetValue("specularGlossinessTexture", out Newtonsoft.Json.Linq.JToken token))
                {
                    if (token.FirstOrDefault(t => (t as Newtonsoft.Json.Linq.JProperty)?.Name == "index") is Newtonsoft.Json.Linq.JProperty indexToken)
                    {
                        specularGlossinessTextureIndex = (int)indexToken.Value;
                    }
                }
            }

            MaterialAttributes materialAttributes = new MaterialAttributes();

            if (diffuseTextureIndex.HasValue)
            {
                Texture diffuseTexture = await GetTextureAsync(gltf, buffers, diffuseTextureIndex.Value);
                materialAttributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor(diffuseTexture));
            }
            else
            {
                float[] baseColor = material.PbrMetallicRoughness.BaseColorFactor;
                Vector4 color = new Vector4(baseColor[0], baseColor[1], baseColor[2], baseColor[3]);
                materialAttributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color));
            }

            if (metallicRoughnessTextureIndex.HasValue)
            {
                Texture metallicRoughnessTexture = await GetTextureAsync(gltf, buffers, metallicRoughnessTextureIndex.Value);
                materialAttributes.MicroSurface = new MaterialRoughnessMapFeature(new ComputeTextureScalar(metallicRoughnessTexture, ColorChannel.G));
                materialAttributes.Specular = new MaterialMetalnessMapFeature(new ComputeTextureScalar(metallicRoughnessTexture, ColorChannel.B));
            }
            else if (specularGlossinessTextureIndex.HasValue)
            {
                Texture specularGlossinessTexture = await GetTextureAsync(gltf, buffers, specularGlossinessTextureIndex.Value);
                materialAttributes.MicroSurface = new MaterialRoughnessMapFeature(new ComputeTextureScalar(specularGlossinessTexture, ColorChannel.A)) { Invert = true };
                materialAttributes.Specular = new MaterialSpecularMapFeature(new ComputeTextureColor(specularGlossinessTexture));
            }
            else
            {
                float roughness = material.PbrMetallicRoughness.RoughnessFactor;
                float metalness = material.PbrMetallicRoughness.MetallicFactor;

                materialAttributes.MicroSurface = new MaterialRoughnessMapFeature(new ComputeScalar(roughness));
                materialAttributes.Specular = new MaterialMetalnessMapFeature(new ComputeScalar(metalness));
            }

            if (normalTextureIndex.HasValue)
            {
                Texture normalTexture = await GetTextureAsync(gltf, buffers, normalTextureIndex.Value);
                materialAttributes.Surface = new MaterialNormalMapFeature(new ComputeTextureColor(normalTexture));
            }

            return materialAttributes;
        }

        private async Task<Texture> GetTextureAsync(Gltf gltf, IList<byte[]> buffers, int textureIndex)
        {
            int imageIndex = gltf.Textures[textureIndex].Source ?? throw new Exception();
            GltfLoader.Schema.Image image = gltf.Images[imageIndex];

            int bufferViewIndex = image.BufferView ?? throw new Exception();
            BufferView bufferView = gltf.BufferViews[bufferViewIndex];

            byte[] currentBuffer = buffers[bufferView.Buffer];

            MemoryStream stream = new MemoryStream(currentBuffer, bufferView.ByteOffset, bufferView.ByteLength);
            Texture texture = await Texture.LoadAsync(GraphicsDevice, stream);
            return texture;
        }

        private async Task<IList<Mesh>> GetMeshesAsync(Gltf gltf, IList<byte[]> buffers)
        {
            List<Mesh> meshes = new List<Mesh>(gltf.Meshes.Length);

            for (int i = 0; i < gltf.Meshes.Length; i++)
            {
                Mesh mesh = await GetMeshAsync(gltf, buffers, i);
                meshes.Add(mesh);
            }

            return meshes;
        }

        private Task<Mesh> GetMeshAsync(Gltf gltf, IList<byte[]> buffers, int meshIndex)
        {
            GltfLoader.Schema.Mesh mesh = gltf.Meshes[meshIndex];

            Span<byte> indexBuffer = Span<byte>.Empty;
            IndexBufferView? indexBufferView = null;
            bool is32bitIndex = false;

            if (mesh.Primitives[0].Indices.HasValue)
            {
                indexBuffer = GetIndexBuffer(gltf, buffers, mesh, out PixelFormat format);
                is32bitIndex = format == PixelFormat.R32_UInt;
                Graphics.Buffer indexBufferResource = Graphics.Buffer.Index.New(GraphicsDevice, indexBuffer).DisposeBy(GraphicsDevice);
                indexBufferView = Graphics.Buffer.Index.CreateIndexBufferView(indexBufferResource, format, indexBufferResource.SizeInBytes);
            }

            VertexBufferView[] vertexBufferViews = GetVertexBufferViews(gltf, buffers, mesh, indexBuffer, is32bitIndex);

            int materialIndex = 0;

            if (mesh.Primitives[0].Material.HasValue)
            {
                materialIndex = mesh.Primitives[0].Material ?? throw new Exception();
            }

            Node node = gltf.Nodes.First(n => n.Mesh == meshIndex);
            float[] matrix = node.Matrix;

            Matrix4x4 worldMatrix = Matrix4x4.Transpose(new Matrix4x4(
                matrix[0], matrix[1], matrix[2], matrix[3],
                matrix[4], matrix[5], matrix[6], matrix[7],
                matrix[8], matrix[9], matrix[10], matrix[11],
                matrix[12], matrix[13], matrix[14], matrix[15]));

            float[] s = node.Scale;
            float[] r = node.Rotation;
            float[] t = node.Translation;

            Vector3 scale = new Vector3(s[0], s[1], s[2]);
            Quaternion rotation = new Quaternion(r[0], r[1], r[2], r[3]);
            Vector3 translation = new Vector3(t[0], t[1], t[2]);

            worldMatrix *= Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(translation);

            MeshDraw meshDraw = new MeshDraw
            {
                IndexBufferView = indexBufferView,
                VertexBufferViews = vertexBufferViews
            };

            return Task.FromResult(new Mesh(meshDraw) { MaterialIndex = materialIndex, WorldMatrix = worldMatrix });
        }

        private VertexBufferView[] GetVertexBufferViews(Gltf gltf, IList<byte[]> buffers, GltfLoader.Schema.Mesh mesh, Span<byte> indexBuffer = default, bool is32bitIndex = false)
        {
            VertexBufferView[] vertexBufferViews = new VertexBufferView[4];

            Dictionary<string, int> attributes = mesh.Primitives[0].Attributes;

            bool hasPosition = attributes.TryGetValue("POSITION", out int positionIndex);
            bool hasNormal = attributes.TryGetValue("NORMAL", out int normalIndex);
            bool hasTangent = attributes.TryGetValue("TANGENT", out int tangentIndex);
            bool hasTexCoord0 = attributes.TryGetValue("TEXCOORD_0", out int texCoord0Index);

            if (hasPosition)
            {
                Span<byte> positionBuffer = GetVertexBuffer(gltf, buffers, positionIndex, out int positionStride);
                Graphics.Buffer positionVertexBuffer = Graphics.Buffer.Vertex.New(GraphicsDevice, positionBuffer).DisposeBy(GraphicsDevice);
                vertexBufferViews[0] = Graphics.Buffer.Vertex.CreateVertexBufferView(positionVertexBuffer, positionVertexBuffer.SizeInBytes, positionStride);

                if (hasNormal)
                {
                    Span<byte> normalBuffer = GetVertexBuffer(gltf, buffers, normalIndex, out int normalStride);
                    Graphics.Buffer normalVertexBuffer = Graphics.Buffer.Vertex.New(GraphicsDevice, normalBuffer).DisposeBy(GraphicsDevice);
                    vertexBufferViews[1] = Graphics.Buffer.Vertex.CreateVertexBufferView(normalVertexBuffer, normalVertexBuffer.SizeInBytes, normalStride);
                }

                if (hasTangent)
                {
                    Span<byte> tangentBuffer = GetVertexBuffer(gltf, buffers, tangentIndex, out int tangentStride);
                    Graphics.Buffer tangentVertexBuffer = Graphics.Buffer.Vertex.New(GraphicsDevice, tangentBuffer).DisposeBy(GraphicsDevice);
                    vertexBufferViews[2] = Graphics.Buffer.Vertex.CreateVertexBufferView(tangentVertexBuffer, tangentVertexBuffer.SizeInBytes, tangentStride);
                }

                if (hasTexCoord0)
                {
                    Span<byte> texCoord0Buffer = GetVertexBuffer(gltf, buffers, texCoord0Index, out int texCoord0Stride);
                    Graphics.Buffer texCoord0VertexBuffer = Graphics.Buffer.Vertex.New(GraphicsDevice, texCoord0Buffer).DisposeBy(GraphicsDevice);
                    vertexBufferViews[3] = Graphics.Buffer.Vertex.CreateVertexBufferView(texCoord0VertexBuffer, texCoord0VertexBuffer.SizeInBytes, texCoord0Stride);

                    if (!hasTangent)
                    {
                        Span<Vector4> tangentBuffer = GenerateTangents(positionBuffer, texCoord0Buffer, indexBuffer, is32bitIndex);
                        Graphics.Buffer tangentVertexBuffer = Graphics.Buffer.Vertex.New(GraphicsDevice, tangentBuffer).DisposeBy(GraphicsDevice);
                        vertexBufferViews[2] = Graphics.Buffer.Vertex.CreateVertexBufferView<Vector4>(tangentVertexBuffer, tangentVertexBuffer.SizeInBytes);
                    }
                }
            }

            return vertexBufferViews;
        }

        private static unsafe Span<Vector4> GenerateTangents(Span<byte> positionBuffer, Span<byte> texCoordBuffer, Span<byte> indexBuffer = default, bool is32bitIndex = false)
        {
            Span<Vector4> tangentBuffer = new Vector4[positionBuffer.Length / sizeof(Vector3)];

            Span<ushort> indexBuffer16;
            Span<int> indexBuffer32;

            fixed (byte* indexBufferPointer = indexBuffer)
            {
                indexBuffer16 = !indexBuffer.IsEmpty && !is32bitIndex ? new Span<ushort>(indexBufferPointer, indexBuffer.Length / sizeof(ushort)) : default;
                indexBuffer32 = !indexBuffer.IsEmpty && is32bitIndex ? new Span<int>(indexBufferPointer, indexBuffer.Length / sizeof(int)) : default;
            }

            Span<Vector3> posBuffer;

            fixed (byte* positionBufferPointer = positionBuffer)
            {
                posBuffer = new Span<Vector3>(positionBufferPointer, positionBuffer.Length);
            }

            Span<Vector2> uvBuffer;

            fixed (byte* texCoordBufferPointer = texCoordBuffer)
            {
                uvBuffer = new Span<Vector2>(texCoordBufferPointer, texCoordBuffer.Length);
            }

            int indexCount = indexBuffer.IsEmpty
                ? positionBuffer.Length / sizeof(Vector3)
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

        private static Span<byte> GetIndexBuffer(Gltf gltf, IList<byte[]> buffers, GltfLoader.Schema.Mesh mesh, out PixelFormat format)
        {
            int indicesIndex = mesh.Primitives[0].Indices ?? throw new Exception();
            Accessor accessor = gltf.Accessors[indicesIndex];

            int bufferViewIndex = accessor.BufferView ?? throw new Exception();
            BufferView bufferView = gltf.BufferViews[bufferViewIndex];

            int offset = bufferView.ByteOffset + accessor.ByteOffset;
            int stride;

            (format, stride) = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.UInt16 => (PixelFormat.R16_UInt, GetCountOfAccessorType(accessor.Type) * sizeof(ushort)),
                Accessor.ComponentTypeEnum.UInt32 => (PixelFormat.R32_UInt, GetCountOfAccessorType(accessor.Type) * sizeof(uint)),
                _ => throw new NotSupportedException("This component type is not supported.")
            };

            return buffers[bufferView.Buffer].AsSpan(offset, stride * accessor.Count);
        }

        private static Span<byte> GetVertexBuffer(Gltf gltf, IList<byte[]> buffers, int accessorIndex, out int stride)
        {
            Accessor accessor = gltf.Accessors[accessorIndex];

            int bufferViewIndex = accessor.BufferView ?? throw new Exception();
            BufferView bufferView = gltf.BufferViews[bufferViewIndex];

            int offset = bufferView.ByteOffset + accessor.ByteOffset;

            stride = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.Float => GetCountOfAccessorType(accessor.Type) * sizeof(float),
                _ => throw new NotSupportedException("This component type is not supported.")
            };

            return buffers[bufferView.Buffer].AsSpan(offset, stride * accessor.Count);
        }
    }
}
