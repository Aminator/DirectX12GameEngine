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
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Rendering;
using DirectX12GameEngine.Rendering.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Engine
{
    public sealed class ContentManager
    {
        private readonly Dictionary<string, Task<object>> pendingTasks = new Dictionary<string, Task<object>>();

        public ContentManager(IServiceProvider services)
        {
            GraphicsDevice = services.GetRequiredService<GraphicsDevice>();
            ModelLoader = services.GetRequiredService<GltfModelLoader>();
        }

        public Assembly[] Assemblies { get; } = AppDomain.CurrentDomain.GetAssemblies();

        public GraphicsDevice GraphicsDevice { get; }

        public Dictionary<string, object> LoadedAssets { get; } = new Dictionary<string, object>();

        internal GltfModelLoader ModelLoader { get; }

        private static int GetIndex(ref string filePath)
        {
            Match match = Regex.Match(filePath, @"\[\d+\]$");
            int index = 0;

            if (match.Success)
            {
                filePath = filePath.Remove(match.Index);
                index = int.Parse(match.Value.Trim('[', ']'));
            }

            return index;
        }

        public async Task<T> LoadAsync<T>(string filePath)
        {
            return (T)await LoadAsync(typeof(T), filePath);
        }

        public async Task<object> LoadAsync(Type type, string filePath)
        {
            if (pendingTasks.TryGetValue(filePath, out Task<object> task))
            {
                return await task;
            }

            if (!LoadedAssets.TryGetValue(filePath, out object asset))
            {
                if (Path.GetExtension(filePath) == ".xml")
                {
                    Task<object> assetTask = LoadElementAsync(filePath);
                    pendingTasks.Add(filePath, assetTask);

                    asset = await assetTask;
                    LoadedAssets.Add(filePath, asset);
                }
                else
                {
                    asset = await LoadFileAsync(type, filePath);
                }
            }

            return asset;
        }

        private async Task<object> InitializeAssetAsync(object asset)
        {
            if (asset is Material material && material.Descriptor != null)
            {
                asset = await Task.Run(() => Material.Create(GraphicsDevice, material.Descriptor));
            }

            return asset;
        }

        private async Task<object> LoadFileAsync(Type type, string filePath)
        {
            int index = GetIndex(ref filePath);
            string extension = Path.GetExtension(filePath);

            if (type == typeof(Model))
            {
                if (extension == ".gltf" || extension == ".glb")
                {
                    return await ModelLoader.LoadModelAsync(filePath);
                }
            }
            else if (type == typeof(Material) || type == typeof(MaterialDescriptor) || type == typeof(MaterialAttributes))
            {
                if (extension == ".gltf" || extension == ".glb")
                {
                    MaterialAttributes materialAttributes = await ModelLoader.LoadMaterialAsync(filePath, index);

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
                    return await Texture.LoadAsync(GraphicsDevice, filePath);
                }
            }

            throw new ArgumentException("This type could not be loaded.");
        }

        private async Task<object> LoadElementAsync(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            XElement root = XElement.Load(stream, LoadOptions.None);
            return await GetParsedElementAsync(root);
        }

        private async Task<object> GetParsedElementAsync(XElement element, Type? type = null)
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

                    if (propertyInfo.GetValue(parsedElement) is IList propertyList)
                    {
                        List<Task<object>> propertyTaskList = new List<Task<object>>();

                        foreach (XElement innerPropertyElement in innerElement.Elements())
                        {
                            //var elem = await GetParsedElementAsync(innerPropertyElement);
                            //propertyList.Add(elem);
                            //propertyTaskList.Add(Task.Run(() => GetParsedElementAsync(innerPropertyElement)));
                            propertyTaskList.Add(GetParsedElementAsync(innerPropertyElement));
                        }

                        object[] loadedElements = await Task.WhenAll(propertyTaskList);

                        foreach (object loadedElement in loadedElements)
                        {
                            propertyList.Add(loadedElement);
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
                else if (parsedElement is IList elementList)
                {
                    //var elem = await GetParsedElementAsync(innerElement);
                    //elementList.Add(elem);
                    //taskList.Add(Task.Run(() => GetParsedElementAsync(innerElement)));
                    taskList.Add(GetParsedElementAsync(innerElement));
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

            return parsedElement;
        }

        private async Task<object> ParseElementAsync(XElement element, Type? type = null)
        {
            if (type is null)
            {
                string typeName = element.Name.NamespaceName + Type.Delimiter + element.Name.LocalName;
                type = Assemblies.SelectMany(a => a.GetTypes()).SingleOrDefault(t => t.FullName == typeName);
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
            else if (typeof(Enum).IsAssignableFrom(type))
            {
                return Enum.Parse(type, value);
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
            else if (type == typeof(float))
            {
                return float.Parse(value);
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

            throw new ArgumentException("This type could not be parsed.");
        }
    }
}
