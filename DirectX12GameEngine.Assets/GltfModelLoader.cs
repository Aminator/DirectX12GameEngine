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

using Mesh = DirectX12GameEngine.Rendering.Mesh;
using MeshDraw = DirectX12GameEngine.Rendering.MeshDraw;
using Texture = DirectX12GameEngine.Graphics.Texture;

namespace DirectX12GameEngine.Assets
{
    internal class GltfModelLoader
    {
        private readonly Gltf gltf;
        private readonly byte[][] buffers;

        private GltfModelLoader(GraphicsDevice device, Gltf gltf, byte[][] buffers)
        {
            GraphicsDevice = device;
            this.gltf = gltf;
            this.buffers = buffers;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public static async Task<GltfModelLoader> CreateAsync(GraphicsDevice device, Stream stream)
        {
            var (gltf, buffers) = await GetGltfModelAndBuffersAsync(stream);

            return new GltfModelLoader(device, gltf, buffers);
        }

        public async Task<IList<MaterialAttributes>> GetMaterialAttributesAsync()
        {
            List<MaterialAttributes> materials = new List<MaterialAttributes>(gltf.Materials.Length);

            for (int i = 0; i < gltf.Materials.Length; i++)
            {
                MaterialAttributes material = await GetMaterialAttributesAsync(i);
                materials.Add(material);
            }

            return materials;
        }

        public async Task<MaterialAttributes> GetMaterialAttributesAsync(int materialIndex)
        {
            Material material = gltf.Materials[materialIndex];

            int? diffuseTextureIndex = material.PbrMetallicRoughness.BaseColorTexture?.Index;
            int? metallicRoughnessTextureIndex = material.PbrMetallicRoughness.MetallicRoughnessTexture?.Index;
            int? normalTextureIndex = material.NormalTexture?.Index;

            if (!diffuseTextureIndex.HasValue)
            {
                if (material.Extensions?.FirstOrDefault().Value is Newtonsoft.Json.Linq.JObject jObject && jObject.TryGetValue("diffuseTexture", out Newtonsoft.Json.Linq.JToken? token))
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
                if (material.Extensions?.FirstOrDefault().Value is Newtonsoft.Json.Linq.JObject jObject && jObject.TryGetValue("specularGlossinessTexture", out Newtonsoft.Json.Linq.JToken? token))
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
                Texture diffuseTexture = await GetTextureAsync(diffuseTextureIndex.Value);
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
                Texture metallicRoughnessTexture = await GetTextureAsync(metallicRoughnessTextureIndex.Value);
                materialAttributes.MicroSurface = new MaterialRoughnessMapFeature(new ComputeTextureScalar(metallicRoughnessTexture, ColorChannel.G));
                materialAttributes.Specular = new MaterialMetalnessMapFeature(new ComputeTextureScalar(metallicRoughnessTexture, ColorChannel.B));
            }
            else if (specularGlossinessTextureIndex.HasValue)
            {
                Texture specularGlossinessTexture = await GetTextureAsync(specularGlossinessTextureIndex.Value);
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
                Texture normalTexture = await GetTextureAsync(normalTextureIndex.Value);
                materialAttributes.Surface = new MaterialNormalMapFeature(new ComputeTextureColor(normalTexture));
            }

            return materialAttributes;
        }

        public async Task<Texture> GetTextureAsync(int textureIndex)
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

        public async Task<IList<Mesh>> GetMeshesAsync()
        {
            List<Mesh> meshes = new List<Mesh>(gltf.Meshes.Length);

            for (int i = 0; i < gltf.Meshes.Length; i++)
            {
                Mesh mesh = await GetMeshAsync(i);
                meshes.Add(mesh);
            }

            return meshes;
        }

        public Task<Mesh> GetMeshAsync(int meshIndex)
        {
            GltfLoader.Schema.Mesh mesh = gltf.Meshes[meshIndex];

            Span<byte> indexBuffer = Span<byte>.Empty;
            GraphicsBuffer<byte>? indexBufferView = null;
            bool is32bitIndex = false;

            if (mesh.Primitives[0].Indices.HasValue)
            {
                indexBuffer = GetIndexBuffer(mesh, out int stride);
                is32bitIndex = stride == sizeof(int);
                indexBufferView = GraphicsBuffer.New(GraphicsDevice, indexBuffer, stride, GraphicsBufferFlags.IndexBuffer).DisposeBy(GraphicsDevice);
            }

            GraphicsBuffer[] vertexBufferViews = GetVertexBufferViews(mesh, indexBuffer, is32bitIndex);

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

        private GraphicsBuffer[] GetVertexBufferViews(GltfLoader.Schema.Mesh mesh, Span<byte> indexBuffer = default, bool is32bitIndex = false)
        {
            GraphicsBuffer[] vertexBufferViews = new GraphicsBuffer[4];

            Dictionary<string, int> attributes = mesh.Primitives[0].Attributes;

            bool hasPosition = attributes.TryGetValue("POSITION", out int positionIndex);
            bool hasNormal = attributes.TryGetValue("NORMAL", out int normalIndex);
            bool hasTangent = attributes.TryGetValue("TANGENT", out int tangentIndex);
            bool hasTexCoord0 = attributes.TryGetValue("TEXCOORD_0", out int texCoord0Index);

            if (hasPosition)
            {
                Span<byte> positionBuffer = GetVertexBuffer(positionIndex, out int positionStride);
                vertexBufferViews[0] = GraphicsBuffer.New(GraphicsDevice, positionBuffer, positionStride, GraphicsBufferFlags.VertexBuffer).DisposeBy(GraphicsDevice);

                if (hasNormal)
                {
                    Span<byte> normalBuffer = GetVertexBuffer(normalIndex, out int normalStride);
                    vertexBufferViews[1] = GraphicsBuffer.New(GraphicsDevice, normalBuffer, normalStride, GraphicsBufferFlags.VertexBuffer).DisposeBy(GraphicsDevice);
                }

                if (hasTangent)
                {
                    Span<byte> tangentBuffer = GetVertexBuffer(tangentIndex, out int tangentStride);
                    vertexBufferViews[2] = GraphicsBuffer.New(GraphicsDevice, tangentBuffer, tangentStride, GraphicsBufferFlags.VertexBuffer).DisposeBy(GraphicsDevice);
                }

                if (hasTexCoord0)
                {
                    Span<byte> texCoord0Buffer = GetVertexBuffer(texCoord0Index, out int texCoord0Stride);
                    vertexBufferViews[3] = GraphicsBuffer.New(GraphicsDevice, texCoord0Buffer, texCoord0Stride, GraphicsBufferFlags.VertexBuffer).DisposeBy(GraphicsDevice);

                    if (!hasTangent)
                    {
                        Span<Vector4> tangentBuffer = VertexHelper.GenerateTangents(positionBuffer, texCoord0Buffer, indexBuffer, is32bitIndex);
                        vertexBufferViews[2] = GraphicsBuffer.New(GraphicsDevice, tangentBuffer, GraphicsBufferFlags.VertexBuffer).DisposeBy(GraphicsDevice);
                    }
                }
            }

            return vertexBufferViews;
        }

        private Span<byte> GetIndexBuffer(GltfLoader.Schema.Mesh mesh, out int stride)
        {
            int indicesIndex = mesh.Primitives[0].Indices ?? throw new Exception();
            Accessor accessor = gltf.Accessors[indicesIndex];

            int bufferViewIndex = accessor.BufferView ?? throw new Exception();
            BufferView bufferView = gltf.BufferViews[bufferViewIndex];

            int offset = bufferView.ByteOffset + accessor.ByteOffset;

            stride = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.UInt16 => GetCountOfAccessorType(accessor.Type) * sizeof(ushort),
                Accessor.ComponentTypeEnum.UInt32 => GetCountOfAccessorType(accessor.Type) * sizeof(uint),
                _ => throw new NotSupportedException("This component type is not supported.")
            };

            return buffers[bufferView.Buffer].AsSpan(offset, stride * accessor.Count);
        }

        private Span<byte> GetVertexBuffer(int accessorIndex, out int stride)
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

        private static Task<Gltf> LoadGltfModelAsync(string filePath)
        {
            return Task.Run(() => Interface.LoadModel(filePath));
        }

        private static async Task<Gltf> LoadGltfModelAsync(Stream stream)
        {
            Gltf gltf = await Task.Run(() => Interface.LoadModel(stream));
            return gltf;
        }

        private static async Task<(Gltf, byte[][])> GetGltfModelAndBuffersAsync(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            using MemoryStream memoryStream1 = new MemoryStream(buffer);
            using MemoryStream memoryStream2 = new MemoryStream(buffer);

            Gltf gltf = await LoadGltfModelAsync(memoryStream1);
            byte[][] buffers = GetGltfModelBuffers(gltf, memoryStream2);

            return (gltf, buffers);
        }

        private static byte[][] GetGltfModelBuffers(Gltf gltf, string filePath)
        {
            byte[][] buffers = new byte[gltf.Buffers.Length][];

            for (int i = 0; i < gltf.Buffers.Length; i++)
            {
                buffers[i] = gltf.LoadBinaryBuffer(i, Path.Combine(Path.GetDirectoryName(filePath), gltf.Buffers[i].Uri ?? Path.GetFileName(filePath)));
            }

            return buffers;
        }

        private static byte[][] GetGltfModelBuffers(Gltf gltf, Stream stream)
        {
            byte[][] buffers = new byte[gltf.Buffers.Length][];

            for (int i = 0; i < gltf.Buffers.Length; i++)
            {
                buffers[i] = Interface.LoadBinaryBuffer(stream);
            }

            return buffers;
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
    }
}
