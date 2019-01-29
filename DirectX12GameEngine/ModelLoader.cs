using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using GltfLoader;
using GltfLoader.Schema;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DirectX12GameEngine
{
    internal static class ModelLoader
    {
        public static GraphicsDevice GraphicsDevice { get; set; }

        public static async Task<Model> LoadGltfModelAsync(string filePath)
        {
            Gltf gltf = await Task.Run(() => Interface.LoadModel(filePath));

            byte[][] buffers = new byte[gltf.Buffers.Length][];

            for (int i = 0; i < gltf.Buffers.Length; i++)
            {
                buffers[i] = gltf.LoadBinaryBuffer(i, gltf.Buffers[i].Uri ?? filePath);
            }

            Model model = new Model();

            IList<Material> materials = await GetMaterialsAsync(gltf, buffers);
            model.Materials.AddRange(materials);

            IList<Mesh> meshes = GetMeshes(gltf, buffers);
            model.Meshes.AddRange(meshes);

            return model;
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

        private static async Task<IList<Material>> GetMaterialsAsync(Gltf gltf, IList<byte[]> buffers)
        {
            List<Material> materials = new List<Material>(gltf.Materials.Length);

            foreach (GltfLoader.Schema.Material material in gltf.Materials)
            {
                int? textureIndex = material.PbrMetallicRoughness.BaseColorTexture?.Index;

                if (!textureIndex.HasValue)
                {
                    if (material.Extensions?.FirstOrDefault().Value is Newtonsoft.Json.Linq.JObject jObject && jObject.TryGetValue("diffuseTexture", out Newtonsoft.Json.Linq.JToken token))
                    {
                        if (token.FirstOrDefault(t => (t as Newtonsoft.Json.Linq.JProperty)?.Name == "index") is Newtonsoft.Json.Linq.JProperty indexToken)
                        {
                            textureIndex = (int)indexToken.Value;
                        }
                    }
                }

                if (textureIndex.HasValue)
                {
                    int imageIndex = gltf.Textures[textureIndex.Value].Source ?? throw new Exception();
                    Image image = gltf.Images[imageIndex];

                    int bufferViewIndex = image.BufferView ?? throw new Exception();
                    BufferView bufferView = gltf.BufferViews[bufferViewIndex];

                    byte[] currentBuffer = buffers[bufferView.Buffer];

                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
                    await randomAccessStream.WriteAsync(currentBuffer.AsBuffer(bufferView.ByteOffset, bufferView.ByteLength));
                    randomAccessStream.Seek(0);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                    PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync();

                    byte[] imageBuffer = pixelDataProvider.DetachPixelData();

                    Format pixelFormat = decoder.BitmapPixelFormat switch
                    {
                        BitmapPixelFormat.Rgba8 => Format.R8G8B8A8_UNorm,
                        BitmapPixelFormat.Bgra8 => Format.B8G8R8A8_UNorm,
                        _ => throw new NotSupportedException("This format is not supported.")
                    };

                    Texture texture = Texture.CreateTexture2D(GraphicsDevice, imageBuffer.AsSpan(), pixelFormat, (int)decoder.PixelWidth, (int)decoder.PixelHeight);

                    MaterialDescriptor descriptor = new MaterialDescriptor();
                    descriptor.Attributes.Diffuse = new ComputeTextureColor(texture);

                    Material mat = new Material(GraphicsDevice, descriptor);
                    materials.Add(mat);
                }
                else
                {
                    float[] baseColor = material.PbrMetallicRoughness.BaseColorFactor;

                    Vector4 color = new Vector4(baseColor[0], baseColor[1], baseColor[2], baseColor[3]);

                    MaterialDescriptor descriptor = new MaterialDescriptor();
                    descriptor.Attributes.Diffuse = new ComputeColor(color);

                    Material mat = new Material(GraphicsDevice, descriptor);
                    materials.Add(mat);
                }
            }

            return materials;
        }

        private static IList<Mesh> GetMeshes(Gltf gltf, IList<byte[]> buffers)
        {
            List<Mesh> meshes = new List<Mesh>(gltf.Meshes.Length);

            for (int i = 0; i < gltf.Meshes.Length; i++)
            {
                GltfLoader.Schema.Mesh mesh = gltf.Meshes[i];

                Dictionary<string, int> attributes = mesh.Primitives[0].Attributes;

                VertexBufferView[] vertexBufferViews = new VertexBufferView[attributes.Count];
                IndexBufferView? indexBufferView = null;

                attributes.TryGetValue("POSITION", out int positionIndex);
                attributes.TryGetValue("NORMAL", out int normalIndex);
                attributes.TryGetValue("TEXCOORD_0", out int texCoordIndex);

                VertexBufferView positions = GetVertexBufferView(gltf, buffers, positionIndex);
                VertexBufferView normals = GetVertexBufferView(gltf, buffers, normalIndex);
                VertexBufferView texCoords = GetVertexBufferView(gltf, buffers, texCoordIndex);

                vertexBufferViews[0] = positions;
                vertexBufferViews[1] = normals;
                vertexBufferViews[2] = texCoords;

                if (mesh.Primitives[0].Indices.HasValue)
                {
                    int indicesIndex = mesh.Primitives[0].Indices ?? throw new Exception();
                    Accessor accessor = gltf.Accessors[indicesIndex];

                    int bufferViewIndex = accessor.BufferView ?? throw new Exception();
                    BufferView bufferView = gltf.BufferViews[bufferViewIndex];

                    int offset = bufferView.ByteOffset + accessor.ByteOffset;

                    (Format format, int stride) = accessor.ComponentType switch
                    {
                        Accessor.ComponentTypeEnum.UInt16 => (Format.R16_UInt, GetCountOfAccessorType(accessor.Type) * sizeof(ushort)),
                        Accessor.ComponentTypeEnum.UInt32 => (Format.R32_UInt, GetCountOfAccessorType(accessor.Type) * sizeof(uint)),
                        _ => throw new NotSupportedException("This component type is not supported.")
                    };

                    Span<byte> currentBuffer = buffers[bufferView.Buffer].AsSpan(offset, stride * accessor.Count);

                    indexBufferView = Texture.CreateIndexBufferView(GraphicsDevice, currentBuffer, format, out Texture indexBuffer);
                    indexBuffer.DisposeBy(GraphicsDevice);
                }

                int materialIndex = 0;

                if (mesh.Primitives[0].Material.HasValue)
                {
                    materialIndex = mesh.Primitives[0].Material ?? throw new Exception();
                }

                Node node = gltf.Nodes.First(n => n.Mesh == i);
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
                Quaternion quaternion = new Quaternion(r[0], r[1], r[2], r[3]);
                Vector3 translation = new Vector3(t[0], t[1], t[2]);

                worldMatrix *= Matrix4x4.CreateScale(scale)
                    * Matrix4x4.CreateFromQuaternion(quaternion)
                    * Matrix4x4.CreateTranslation(translation);

                meshes.Add(new Mesh
                {
                    VertexBufferViews = vertexBufferViews,
                    MaterialIndex = materialIndex,
                    IndexBufferView = indexBufferView,
                    WorldMatrix = worldMatrix
                });
            }

            return meshes;
        }

        private static VertexBufferView GetVertexBufferView(Gltf gltf, IList<byte[]> buffers, int accessorIndex)
        {
            Accessor accessor = gltf.Accessors[accessorIndex];

            int bufferViewIndex = accessor.BufferView ?? throw new Exception();
            BufferView bufferView = gltf.BufferViews[bufferViewIndex];

            int offset = bufferView.ByteOffset + accessor.ByteOffset;

            int stride = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.Float => GetCountOfAccessorType(accessor.Type) * sizeof(float),
                _ => throw new NotSupportedException("This component type is not supported.")
            };

            Span<byte> currentBuffer = buffers[bufferView.Buffer].AsSpan(offset, stride * accessor.Count);

            VertexBufferView vertexBufferView = Texture.CreateVertexBufferView(GraphicsDevice, currentBuffer, out Texture vertexBuffer, stride);
            vertexBuffer.DisposeBy(GraphicsDevice);

            return vertexBufferView;
        }
    }
}
