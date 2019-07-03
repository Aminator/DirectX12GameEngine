using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        private class DeserializeOperation
        {
            public DeserializeOperation(Reference parentReference)
            {
                ParentReference = parentReference;
            }

            public Reference ParentReference { get; }

            public AsyncDictionary<Guid, IIdentifiable> IdentifiableObjects { get; } = new AsyncDictionary<Guid, IIdentifiable>();
        }

        private async Task<object> DeserializeObjectAsync(string initialPath, string newPath, Type type, object? obj)
        {
            Reference? reference = null;

            if (obj != null)
            {
                reference = FindDeserializedObject(initialPath, type);

                if (reference is null || reference.Object != obj)
                {
                    throw new InvalidOperationException();
                }
            }

            HashSet<Reference>? references = null;

            if (reference != null)
            {
                references = reference.References;
                reference.References = new HashSet<Reference>();

                reference.IsDeserialized = false;
            }

            object asset = await DeserializeObjectAsync(newPath, type, null, obj);

            if (references != null)
            {
                foreach (Reference childReference in references)
                {
                    DecrementReference(childReference, false);
                }
            }

            return asset;
        }

        private async Task<object> DeserializeObjectAsync(string path, Type type, Reference? parentReference, object? obj)
        {
            bool isRoot = parentReference is null;
            Reference? reference = FindDeserializedObject(path, type);

            if (reference != null && reference.IsDeserialized)
            {
                if (isRoot || parentReference!.References.Add(reference))
                {
                    IncrementReference(reference, isRoot);
                }

                return reference.Object;
            }

            if (!await ExistsAsync(path))
            {
                throw new FileNotFoundException();
            }

            XElement root;

            using (Stream stream = await RootFolder.OpenStreamForReadAsync(path + FileExtension))
            {
#if NETSTANDARD2_0
                root = await Task.Run(() => XElement.Load(stream));
#else
                root = await XElement.LoadAsync(stream, LoadOptions.None, default);
#endif
            }

            object result;
            Asset? asset = null;

            Type loadedType = LoadedTypes[root.Name.NamespaceName][root.Name.LocalName];

            if (!type.IsAssignableFrom(loadedType))
            {
                if (typeof(Asset).IsAssignableFrom(loadedType))
                {
                    asset = (Asset)ActivatorUtilities.CreateInstance(Services, loadedType);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            if (reference is null)
            {
                if (obj != null)
                {
                    result = obj;
                }
                else
                {
                    if (type.IsAssignableFrom(loadedType))
                    {
                        result = ActivatorUtilities.CreateInstance(Services, loadedType);
                    }
                    else if (typeof(Asset).IsAssignableFrom(loadedType))
                    {
                        result = ActivatorUtilities.CreateInstance(Services, type);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                reference = new Reference(path, result, isRoot);
                SetAsset(reference);
            }
            else
            {
                result = reference.Object;
            }

            reference.IsDeserialized = true;

            DeserializeOperation operation = new DeserializeOperation(reference);

            if (asset != null)
            {
                await DeserializeAsync(root, operation, asset);
                await asset.CreateAssetAsync(result);
            }
            else
            {
                await DeserializeAsync(root, operation, result);
            }

            parentReference?.References.Add(reference);

            return result;
        }

        private async Task<object> DeserializeAsync(XElement element, DeserializeOperation operation, object? obj = null)
        {
            Type loadedType = LoadedTypes[element.Name.NamespaceName][element.Name.LocalName];

            object parsedElement = obj ?? await ParseElementAsync(element, loadedType, operation);

            await SetPropertiesFromAttributesAsync(element, loadedType, operation, parsedElement);
            await SetPropertiesFromElementsAsync(element, operation, parsedElement);

            if (parsedElement is IIdentifiable identifiable)
            {
                if (!operation.IdentifiableObjects.ContainsKey(identifiable.Id))
                {
                    operation.IdentifiableObjects.Add(identifiable.Id, identifiable);
                }
            }

            return parsedElement;
        }

        private async Task SetPropertiesFromElementsAsync(XElement element, DeserializeOperation operation, object parsedElement)
        {
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
                            propertyTaskList.Add(DeserializeAsync(innerPropertyElement, operation));
                        }

                        object[] loadedElements = await Task.WhenAll(propertyTaskList);

                        while (propertyCollection.Count > 0)
                        {
                            propertyCollection.RemoveAt(propertyCollection.Count - 1);
                        }

                        foreach (object loadedElement in loadedElements)
                        {
                            propertyCollection.Add(loadedElement);
                        }
                    }
                    else if (propertyInfo.CanWrite)
                    {
                        object parsedObject;

                        if (innerElement.HasElements)
                        {
                            XElement propertyElement = innerElement.Elements().First();
                            parsedObject = await DeserializeAsync(propertyElement, operation);
                        }
                        else
                        {
                            parsedObject = await ParseAsync(propertyInfo.PropertyType, innerElement.Value, operation);
                        }

                        propertyInfo.SetValue(parsedElement, parsedObject);
                    }
                    else
                    {
                        throw new InvalidOperationException("A property syntax must be a writable or a list property.");
                    }
                }
                else if (parsedElement is IList)
                {
                    taskList.Add(DeserializeAsync(innerElement, operation));
                }
                else
                {
                    throw new InvalidOperationException("An inner element must be a property syntax or a list element.");
                }
            }

            if (parsedElement is IList list)
            {
                object[] loadedElements = await Task.WhenAll(taskList);

                while (list.Count > 0)
                {
                    list.RemoveAt(list.Count - 1);
                }

                foreach (object loadedElement in loadedElements)
                {
                    list.Add(loadedElement);
                }
            }
        }

        private async Task<object> ParseElementAsync(XElement element, Type type, DeserializeOperation operation)
        {
            string? content = element.Nodes().OfType<XText>().FirstOrDefault()?.Value;

            object parsedElement = string.IsNullOrWhiteSpace(content)
                ? ActivatorUtilities.CreateInstance(Services, type)
                : await ParseAsync(type, content, operation);

            return parsedElement;
        }

        private async Task SetPropertiesFromAttributesAsync(XElement element, Type type, DeserializeOperation operation, object parsedElement)
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (!attribute.IsNamespaceDeclaration)
                {
                    PropertyInfo propertyInfo = type.GetProperty(attribute.Name.LocalName);
                    object parsedObject = await ParseAttributeAsync(propertyInfo.PropertyType, attribute.Value, operation);
                    propertyInfo.SetValue(parsedElement, parsedObject);
                }
            }
        }

        private async Task<object> ParseAttributeAsync(Type type, string value, DeserializeOperation operation)
        {
            string trimmedValue = value.Trim();
            Match match = Regex.Match(trimmedValue, @"\{.+\}");

            return match.Success && match.Index == 0
                ? await DeserializeObjectAsync(trimmedValue.Trim('{', '}'), type, operation.ParentReference, null)
                : await ParseAsync(type, value, operation);
        }

        private async Task<object> ParseAsync(Type type, string value, DeserializeOperation operation)
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

            if (LoadedTypes["http://schemas.directx12gameengine.com/xaml"]["IComputeScalar"].IsAssignableFrom(type))
            {
                float scalar = float.Parse(value);
                return Activator.CreateInstance(LoadedTypes["http://schemas.directx12gameengine.com/xaml"]["ComputeScalar"], scalar);
            }

            if (LoadedTypes["http://schemas.directx12gameengine.com/xaml"]["IComputeColor"].IsAssignableFrom(type))
            {
                float[] vector = Regex.Replace(value, @"\s+", "").Split(',').Select(n => float.Parse(n)).ToArray();

                if (vector.Length == 4)
                {
                    Vector4 color = new Vector4(vector[0], vector[1], vector[2], vector[3]);
                    return Activator.CreateInstance(LoadedTypes["http://schemas.directx12gameengine.com/xaml"]["ComputeColor"], color);
                }
            }

            if (typeof(IIdentifiable).IsAssignableFrom(type))
            {
                if (Guid.TryParse(value, out Guid guid))
                {
                    return await operation.IdentifiableObjects.GetValueAsync(guid);
                }
            }

            return await DeserializeObjectAsync(value, type, operation.ParentReference, null);
        }
    }
}
