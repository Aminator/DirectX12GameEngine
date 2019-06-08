using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public sealed class ContentManager
    {
        private readonly AsyncDictionary<Guid, IIdentifiable> identifiableObjects = new AsyncDictionary<Guid, IIdentifiable>();
        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> loadedAssets = new ConcurrentDictionary<string, Lazy<Task<object>>>();

        public ContentManager(IServiceProvider services)
        {
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
            LoadedTypes = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.GetExportedTypes().Where(t => !(t.IsAbstract && t.IsSealed)));

            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
            ModelLoader = services.GetRequiredService<GltfModelLoader>();
        }

        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (!args.LoadedAssembly.IsDynamic)
            {
                LoadedTypes = LoadedTypes.Concat(args.LoadedAssembly.GetExportedTypes().Where(t => !(t.IsAbstract && t.IsSealed)));
            }
        }

        public GraphicsDevice GraphicsDevice { get; }

        public IEnumerable<Type> LoadedTypes { get; private set; }

        internal GltfModelLoader ModelLoader { get; }

        public async Task<T> LoadAsync<T>(string path)
        {
            return (T)await LoadAsync(typeof(T), path);
        }

        public async Task<object> LoadAsync(Type type, string path)
        {
            if (Path.GetExtension(path) == ".xml")
            {
                return await loadedAssets.GetOrAdd(path, p => new Lazy<Task<object>>(() => DeserializeAsync(p))).Value;
            }
            else
            {
                return await LoadFileAsync(type, path);
            }
        }

        private static int GetIndex(ref string path)
        {
            Match match = Regex.Match(path, @"\[\d+\]$");
            int index = 0;

            if (match.Success)
            {
                path = path.Remove(match.Index);
                index = int.Parse(match.Value.Trim('[', ']'));
            }

            return index;
        }

        private async Task<object> InitializeAssetAsync(object asset)
        {
            if (asset is Material material && material.Descriptor != null)
            {
                asset = await Task.Run(() => Material.Create(GraphicsDevice, material.Descriptor));
            }

            return asset;
        }

        private async Task<object> LoadFileAsync(Type type, string path)
        {
            int index = GetIndex(ref path);
            string extension = Path.GetExtension(path);

            if (type == typeof(Model))
            {
                if (extension == ".gltf" || extension == ".glb")
                {
                    return await ModelLoader.LoadModelAsync(path);
                }
            }
            else if (type == typeof(Material) || type == typeof(MaterialDescriptor) || type == typeof(MaterialAttributes))
            {
                if (extension == ".gltf" || extension == ".glb")
                {
                    MaterialAttributes materialAttributes = await ModelLoader.LoadMaterialAsync(path, index);

                    if (type == typeof(MaterialAttributes))
                    {
                        return materialAttributes;
                    }
                    else if (type == typeof(MaterialDescriptor))
                    {
                        return new MaterialDescriptor { Attributes = materialAttributes };
                    }
                    else if (type == typeof(Material))
                    {
                        return new Material { Descriptor = new MaterialDescriptor { Attributes = materialAttributes } };
                    }
                }
            }
            else if (type == typeof(Texture))
            {
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                {
                    return await Texture.LoadAsync(GraphicsDevice, path);
                }
            }

            throw new ArgumentException("This type could not be loaded.");
        }

        private async Task<object> DeserializeAsync(string path)
        {
            using FileStream stream = File.OpenRead(path);
#if NETSTANDARD2_0
            XElement root = await Task.Run(() => XElement.Load(stream));
#else
            XElement root = await XElement.LoadAsync(stream, LoadOptions.None, default);
#endif
            return await ParseElementAndChildrenAsync(root);
        }

        private async Task<object> ParseElementAndChildrenAsync(XElement element, Type? type = null)
        {
            object parsedElement = await ParseElementAsync(element, type);

            List<Task<object>> taskList = new List<Task<object>>();

            foreach (XElement innerElement in element.Elements())
            {
                bool isProperty = innerElement.Name.LocalName.StartsWith(innerElement.Parent.Name.LocalName + Type.Delimiter);

                if (isProperty)
                {
                    Match match = Regex.Match(innerElement.Name.LocalName, @"[^\.]+$");
                    PropertyInfo propertyInfo = parsedElement.GetType().GetProperty(match.Value);

                    if (propertyInfo.GetValue(parsedElement) is IList propertyCollection)
                    {
                        List<Task<object>> propertyTaskList = new List<Task<object>>();

                        foreach (XElement innerPropertyElement in innerElement.Elements())
                        {
                            propertyTaskList.Add(ParseElementAndChildrenAsync(innerPropertyElement));
                        }

                        object[] loadedElements = await Task.WhenAll(propertyTaskList);

                        foreach (object loadedElement in loadedElements)
                        {
                            propertyCollection.Add(loadedElement);
                        }
                    }
                    else if (propertyInfo.CanWrite)
                    {
                        XElement propertyElement = innerElement.HasElements ? innerElement.Elements().First() : innerElement;
                        Type? propertyType = innerElement.HasElements ? null : propertyInfo.PropertyType;

                        object parsedObject = await ParseElementAndChildrenAsync(propertyElement, propertyType);
                        propertyInfo.SetValue(parsedElement, parsedObject);
                    }
                    else
                    {
                        throw new InvalidOperationException("An inner element must be a writable property or a list element.");
                    }
                }
                else if (parsedElement is IList)
                {
                    taskList.Add(ParseElementAndChildrenAsync(innerElement));
                }
                else
                {
                    throw new InvalidOperationException("An inner element must be a property syntax or a list element.");
                }
            }

            if (parsedElement is IList list)
            {
                object[] loadedElements = await Task.WhenAll(taskList.ToArray());

                foreach (object loadedElement in loadedElements)
                {
                    list.Add(loadedElement);
                }
            }

            parsedElement = await InitializeAssetAsync(parsedElement);

            if (parsedElement is IIdentifiable identifiable)
            {
                if (!identifiableObjects.ContainsKey(identifiable.Id))
                {
                    identifiableObjects.Add(identifiable.Id, identifiable);
                }
            }

            return parsedElement;
        }

        private async Task<object> ParseElementAsync(XElement element, Type? type = null)
        {
            if (type is null)
            {
                string typeName = element.Name.NamespaceName + Type.Delimiter + element.Name.LocalName;
                type = LoadedTypes.SingleOrDefault(t => t.FullName == typeName);
            }

            string? content = element.Nodes().OfType<XText>().FirstOrDefault()?.Value;

            IEnumerable<XAttribute> attributes = element.Attributes();
            XAttribute? contentAttribute = attributes.FirstOrDefault(x => x.Name.LocalName == "Content");

            if (!string.IsNullOrWhiteSpace(contentAttribute?.Value))
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    content = contentAttribute.Value;
                }
                else
                {
                    throw new ArgumentException("The content (inner text or child elements) and the content attribute cannot be set at the same time.");
                }
            }

            object parsedElement = string.IsNullOrWhiteSpace(content)
                ? Activator.CreateInstance(type)
                : await ParseAsync(type, content);

            foreach (XAttribute attribute in attributes)
            {
                if (!attribute.IsNamespaceDeclaration && attribute.Name.LocalName != "Content")
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

            if (typeof(Enum).IsAssignableFrom(type))
            {
                return Enum.Parse(type, value);
            }

            if (type == typeof(Guid))
            {
                return Guid.Parse(value);
            }

            if (type == typeof(Uri))
            {
                return new Uri(value);
            }

            if (type == typeof(bool))
            {
                return bool.Parse(value);
            }

            if (type == typeof(int))
            {
                return int.Parse(value);
            }

            if (type == typeof(double))
            {
                return double.Parse(value);
            }

            if (type == typeof(float))
            {
                return float.Parse(value);
            }

            if (type == typeof(Vector3))
            {
                float[] vector = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (vector.Length == 3)
                {
                    return new Vector3(vector[0], vector[1], vector[2]);
                }
            }

            if (type == typeof(Vector4))
            {
                float[] vector = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (vector.Length == 4)
                {
                    return new Vector4(vector[0], vector[1], vector[2], vector[3]);
                }
            }

            if (type == typeof(Quaternion))
            {
                float[] quaternion = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (quaternion.Length == 4)
                {
                    return new Quaternion(quaternion[0], quaternion[1], quaternion[2], quaternion[3]);
                }
            }

            if (type == typeof(Matrix4x4))
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

            if (typeof(IComputeScalar).IsAssignableFrom(type))
            {
                float scalar = float.Parse(value);
                return new ComputeScalar(scalar);
            }

            if (typeof(IComputeColor).IsAssignableFrom(type))
            {
                float[] vector = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (vector.Length == 4)
                {
                    Vector4 color = new Vector4(vector[0], vector[1], vector[2], vector[3]);
                    return new ComputeColor(color);
                }
            }

            if (typeof(IIdentifiable).IsAssignableFrom(type))
            {
                if (Guid.TryParse(value, out Guid guid))
                {
                    return await identifiableObjects.GetValueAsync(guid);
                }
            }

            return await LoadAsync(type, value);
        }
    }
}
