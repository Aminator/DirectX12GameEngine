using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using GltfLoader;
using GltfLoader.Schema;
using Microsoft.Extensions.DependencyInjection;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DirectX12GameEngine
{
    public sealed class ContentManager
    {
        private readonly GraphicsPipelineState colorPipelineState;
        private readonly GraphicsPipelineState texturePipelineState;

        public ContentManager(IServiceProvider services)
        {
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();

            GraphicsDevice.CompileShaders(Path.Combine("Assets", "Shaders", "ColorShader.hlsl"), out SharpDX.D3DCompiler.ShaderBytecode colorVertexShader, out SharpDX.D3DCompiler.ShaderBytecode colorPixelShader);

            InputElement[] colorInputElements = new[]
            {
                new InputElement("Position", 0, Format.R32G32B32_Float, 0),
                new InputElement("Normal", 0, Format.R32G32B32_Float, 1)
            };

            colorPipelineState = new GraphicsPipelineState(GraphicsDevice, colorVertexShader, colorPixelShader, colorInputElements);

            GraphicsDevice.CompileShaders(Path.Combine("Assets", "Shaders", "ModelShader.hlsl"), out SharpDX.D3DCompiler.ShaderBytecode textureVertexShader, out SharpDX.D3DCompiler.ShaderBytecode texturePixelShader);

            InputElement[] textureInputElements = new[]
            {
                new InputElement("Position", 0, Format.R32G32B32_Float, 0),
                new InputElement("Normal", 0, Format.R32G32B32_Float, 1),
                new InputElement("TexCoord", 0, Format.R32G32_Float, 2)
            };

            texturePipelineState = new GraphicsPipelineState(GraphicsDevice, textureVertexShader, texturePixelShader, textureInputElements);

            Assemblies = AppDomain.CurrentDomain.GetAssemblies();

            LoadedTypes = Assemblies.SelectMany(a => a.GetExportedTypes()).ToArray();
        }

        public Assembly[] Assemblies { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public Dictionary<string, object> LoadedAssets { get; } = new Dictionary<string, object>();

        public Type[] LoadedTypes { get; }

        public async Task<T> LoadAsync<T>(string filePath)
        {
            return (T)await LoadAsync(typeof(T), filePath);
        }

        private async Task<object> LoadAsync(Type type, string filePath)
        {
            if (!LoadedAssets.TryGetValue(filePath, out object asset))
            {
                if (type == typeof(Model))
                {
                    asset = await LoadGltfModelAsync(filePath);
                }
                else
                {
                    asset = await LoadElementAsync(filePath);
                }

                LoadedAssets.Add(filePath, asset);
            }

            return asset;
        }

        private async Task<Model> LoadGltfModelAsync(string filePath)
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

        private async Task<object> LoadElementAsync(string filePath)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                XElement root = XElement.Load(streamReader.BaseStream, LoadOptions.None);
                return await GetParsedElementAsync(root);
            }
        }

        private async Task<object> GetParsedElementAsync(XElement element, Type? type = null)
        {
            object parsedElement = await ParseElementAsync(element, type);

            IEnumerable<XElement> innerElements = element.Elements();

            foreach (XElement innerElement in innerElements)
            {
                bool isProperty = innerElement.Name.LocalName.StartsWith(innerElement.Parent.Name.LocalName + Type.Delimiter);

                if (isProperty)
                {
                    Match match = Regex.Match(innerElement.Name.LocalName, @"[^\.]+$");
                    PropertyInfo propertyInfo = parsedElement!.GetType().GetProperty(match.Value);

                    if (propertyInfo.GetValue(parsedElement) is IList propertyList)
                    {
                        IEnumerable<XElement> innerPropertyElements = innerElement.Elements();

                        foreach (XElement innerPropertyElement in innerPropertyElements)
                        {
                            object parsedObject = await GetParsedElementAsync(innerPropertyElement);
                            propertyList.Add(parsedObject);
                        }
                    }
                    else if (propertyInfo.CanWrite)
                    {
                        XElement propertyElement = innerElement.HasElements ? innerElement.Elements().First() : innerElement;
                        Type? propertyType = innerElement.HasElements ? null : propertyInfo.PropertyType;

                        object parsedObject = await GetParsedElementAsync(propertyElement, propertyType);
                        propertyInfo.SetValue(parsedElement, parsedObject);
                    }
                }
                else if (parsedElement is IList list)
                {
                    object parsedObject = await GetParsedElementAsync(innerElement);
                    list.Add(parsedObject);
                }
            }

            return parsedElement;
        }

        private async Task<object> ParseElementAsync(XElement element, Type? type = null)
        {
            if (type == null)
            {
                string typeName = element.Name.NamespaceName + Type.Delimiter + element.Name.LocalName;
                type = LoadedTypes.SingleOrDefault(t => t.FullName == typeName);
            }

            string text = element.Nodes().OfType<XText>().FirstOrDefault()?.Value;

            object parsedElement = string.IsNullOrWhiteSpace(text)
                ? Activator.CreateInstance(type)
                : await ParseAsync(type, text);

            IEnumerable<XAttribute> attributes = element.Attributes();

            foreach (XAttribute attribute in attributes)
            {
                if (!attribute.IsNamespaceDeclaration)
                {
                    PropertyInfo propertyInfo = type.GetProperty(attribute.Name.LocalName);
                    object parsedObject = await ParseAttributeAsync(propertyInfo.PropertyType, attribute.Value);
                    propertyInfo.SetValue(parsedElement, parsedObject);
                }
            }

            return parsedElement;
        }

        private async Task<object> ParseAttributeAsync(Type type, string value)
        {
            string trimmedValue = value.Trim();
            Match match = Regex.Match(trimmedValue, @"\{.+\}");

            return match.Success && match.Index == 0
                ? await LoadAsync(type, trimmedValue.Trim('{', '}'))
                : await ParseAsync(type, value);
        }

        private async Task<object> ParseAsync(Type type, string value)
        {
            if (type == typeof(string))
            {
                return value;
            }
            else if (type == typeof(Uri))
            {
                return new Uri(value);
            }
            else if (type == typeof(bool))
            {
                return bool.Parse(value);
            }
            else if (type == typeof(int))
            {
                return int.Parse(value);
            }
            else if (type == typeof(double))
            {
                return double.Parse(value);
            }
            else if (type == typeof(Vector3))
            {
                float[] vector = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (vector.Length == 3)
                {
                    return new Vector3(vector[0], vector[1], vector[2]);
                }
            }
            else if (type == typeof(Vector4))
            {
                float[] vector = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (vector.Length == 4)
                {
                    return new Vector4(vector[0], vector[1], vector[2], vector[3]);
                }
            }
            else if (type == typeof(Quaternion))
            {
                float[] quaternion = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (quaternion.Length == 4)
                {
                    return new Quaternion(quaternion[0], quaternion[1], quaternion[2], quaternion[3]);
                }
            }
            else if (type == typeof(Matrix4x4))
            {
                float[] matrix = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (matrix.Length == 16)
                {
                    return new Matrix4x4(
                        matrix[0], matrix[1], matrix[2], matrix[3],
                        matrix[4], matrix[5], matrix[6], matrix[7],
                        matrix[8], matrix[9], matrix[10], matrix[11],
                        matrix[12], matrix[13], matrix[14], matrix[15]);
                }
            }
            else
            {
                return await LoadAsync(type, value);
            }

            throw new ArgumentException();
        }

        private int GetCountOfAccessorType(Accessor.TypeEnum type)
        {
            switch (type)
            {
                case Accessor.TypeEnum.Scalar:
                    return 1;
                case Accessor.TypeEnum.Vec2:
                    return 2;
                case Accessor.TypeEnum.Vec3:
                    return 3;
                case Accessor.TypeEnum.Vec4:
                    return 4;
                case Accessor.TypeEnum.Mat2:
                    return 4;
                case Accessor.TypeEnum.Mat3:
                    return 9;
                case Accessor.TypeEnum.Mat4:
                    return 16;
                default:
                    throw new ArgumentException(nameof(type), "This type is not supported.");
            }
        }

        private async Task<IList<Material>> GetMaterialsAsync(Gltf gltf, IList<byte[]> buffers)
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
                    int imageIndex = gltf.Textures[textureIndex.Value].Source.Value;
                    Image image = gltf.Images[imageIndex];

                    int bufferViewIndex = image.BufferView.Value;
                    BufferView bufferView = gltf.BufferViews[bufferViewIndex];

                    byte[] currentBuffer = buffers[bufferView.Buffer];

                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
                    await randomAccessStream.WriteAsync(currentBuffer.AsBuffer(bufferView.ByteOffset, bufferView.ByteLength));
                    randomAccessStream.Seek(0);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                    PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync();

                    byte[] imageBuffer = pixelDataProvider.DetachPixelData();

                    Format pixelFormat;

                    switch (decoder.BitmapPixelFormat)
                    {
                        case BitmapPixelFormat.Rgba8:
                            pixelFormat = Format.R8G8B8A8_UNorm;
                            break;
                        case BitmapPixelFormat.Bgra8:
                            pixelFormat = Format.B8G8R8A8_UNorm;
                            break;
                        default:
                            throw new ArgumentException("This format is not supported.");
                    }

                    Texture texture = Texture.CreateTexture2D(GraphicsDevice, imageBuffer.AsSpan(), pixelFormat, (int)decoder.PixelWidth, (int)decoder.PixelHeight);

                    Material mat = new Material(texturePipelineState);
                    mat.Textures.Add(texture);

                    materials.Add(mat);
                }
                else
                {
                    float[] baseColor = material.PbrMetallicRoughness.BaseColorFactor;
                    Texture constantBuffer = Texture.CreateConstantBufferView(GraphicsDevice, baseColor.AsSpan()).DisposeBy(GraphicsDevice);

                    Material mat = new Material(colorPipelineState);
                    mat.Textures.Add(constantBuffer);

                    materials.Add(mat);
                }
            }

            return materials;
        }

        private IList<Mesh> GetMeshes(Gltf gltf, IList<byte[]> buffers)
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
                    int indicesIndex = mesh.Primitives[0].Indices.Value;

                    Accessor accessor = gltf.Accessors[indicesIndex];
                    BufferView bufferView = gltf.BufferViews[accessor.BufferView.Value];

                    int offset = bufferView.ByteOffset + accessor.ByteOffset;

                    Format format;
                    int stride;

                    switch (accessor.ComponentType)
                    {
                        case Accessor.ComponentTypeEnum.UInt16:
                            format = Format.R16_UInt;
                            stride = GetCountOfAccessorType(accessor.Type) * sizeof(ushort);
                            break;
                        case Accessor.ComponentTypeEnum.UInt32:
                            format = Format.R32_UInt;
                            stride = GetCountOfAccessorType(accessor.Type) * sizeof(uint);
                            break;
                        default:
                            throw new ArgumentException("This component type is not supported.");
                    }

                    Span<byte> currentBuffer = buffers[bufferView.Buffer].AsSpan(offset, stride * accessor.Count);

                    indexBufferView = Texture.CreateIndexBufferView(GraphicsDevice, currentBuffer, format, out Texture indexBuffer);
                    indexBuffer.DisposeBy(GraphicsDevice);
                }

                int materialIndex = 0;

                if (mesh.Primitives[0].Material.HasValue)
                {
                    materialIndex = mesh.Primitives[0].Material.Value;
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

        private VertexBufferView GetVertexBufferView(Gltf gltf, IList<byte[]> buffers, int accessorIndex)
        {
            Accessor accessor = gltf.Accessors[accessorIndex];
            BufferView bufferView = gltf.BufferViews[accessor.BufferView.Value];

            int offset = bufferView.ByteOffset + accessor.ByteOffset;

            int stride;

            switch (accessor.ComponentType)
            {
                case Accessor.ComponentTypeEnum.Float:
                    stride = GetCountOfAccessorType(accessor.Type) * sizeof(float);
                    break;
                default:
                    throw new ArgumentException("This component type is not supported.");
            }

            Span<byte> currentBuffer = buffers[bufferView.Buffer].AsSpan(offset, stride * accessor.Count);

            VertexBufferView vertexBufferView = Texture.CreateVertexBufferView(GraphicsDevice, currentBuffer, out Texture vertexBuffer, stride);
            vertexBuffer.DisposeBy(GraphicsDevice);

            return vertexBufferView;
        }
    }
}
