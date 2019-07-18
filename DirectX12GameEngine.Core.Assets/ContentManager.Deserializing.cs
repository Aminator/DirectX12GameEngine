using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        internal class DeserializeOperation
        {
            public DeserializeOperation(Type type)
            {
                Type = type;
            }

            public DeserializeOperation(Type type, Reference parentReference) : this(type)
            {
                ParentReference = parentReference;
            }

            public DeserializeOperation(Type type, Reference parentReference, AsyncDictionary<Guid, IIdentifiable> identifiableObjects) : this(type, parentReference)
            {
                IdentifiableObjects = identifiableObjects;
            }

            public Type Type { get; }

            public Reference? ParentReference { get; }

            public AsyncDictionary<Guid, IIdentifiable> IdentifiableObjects { get; } = new AsyncDictionary<Guid, IIdentifiable>();
        }

        public Task<object> DeserializeAsync(XElement element, object? obj = null)
        {
            DeserializeOperation operation = new DeserializeOperation(null!);
            return DeserializeAsync(element, operation, obj);
        }

        internal async Task<object> DeserializeAsync(string initialPath, string newPath, Type type, object? obj)
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

            object asset = await DeserializeAsync(newPath, type, null, obj);

            if (references != null)
            {
                foreach (Reference childReference in references)
                {
                    DecrementReference(childReference, false);
                }
            }

            return asset;
        }

        internal async Task<object> DeserializeAsync(string path, Type type, Reference? parentReference, object? obj)
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

            Type loadedType = GetTypeFromXmlName(root.Name.NamespaceName, root.Name.LocalName);

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

            DeserializeOperation operation = new DeserializeOperation(type, reference);

            if (asset != null)
            {
                await DeserializeAsync(root, operation, asset);
                await asset.CreateAssetAsync(result, Services);
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
            Type loadedType = GetTypeFromXmlName(element.Name.NamespaceName, element.Name.LocalName);

            object parsedElement = obj ?? ParseElement(element, loadedType);

            await SetPropertiesFromAttributesAsync(element, loadedType, operation, parsedElement);
            await SetPropertiesFromElementsAsync(element, operation, parsedElement);

            if (parsedElement is MarkupExtension markupExtension)
            {
                IServiceProvider services = new ServiceCollection()
                    .AddSingleton(this)
                    .AddSingleton(operation)
                    .AddSingleton(element)
                    .BuildServiceProvider();

                 parsedElement = await markupExtension.ProvideValueAsync(services);
            }

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
                            DeserializeOperation newOperation = new DeserializeOperation(propertyInfo.PropertyType, operation.ParentReference!, operation.IdentifiableObjects);
                            parsedObject = await DeserializeAsync(propertyElement, newOperation);
                        }
                        else
                        {
                            parsedObject = TypeDescriptor.GetConverter(propertyInfo.PropertyType).ConvertFromString(innerElement.Value);
                        }

                        propertyInfo.SetValue(parsedElement, parsedObject);
                    }
                    else
                    {
                        throw new InvalidOperationException("A property syntax must be a writable or a list property.");
                    }
                }
                else if (parsedElement is IList collection)
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

        private async Task SetPropertiesFromAttributesAsync(XElement element, Type type, DeserializeOperation operation, object parsedElement)
        {
            foreach (XAttribute attribute in element.Attributes())
            {
                if (!attribute.IsNamespaceDeclaration)
                {
                    PropertyInfo propertyInfo = type.GetProperty(attribute.Name.LocalName);
                    DeserializeOperation newOperation = new DeserializeOperation(propertyInfo.PropertyType, operation.ParentReference!, operation.IdentifiableObjects);
                    object parsedObject = await ParseAttributeValueAsync(propertyInfo.PropertyType, attribute.Value, element, newOperation);
                    propertyInfo.SetValue(parsedElement, parsedObject);
                }
            }
        }

        private object ParseElement(XElement element, Type type)
        {
            string? content = element.Nodes().OfType<XText>().FirstOrDefault()?.Value;

            return string.IsNullOrWhiteSpace(content)
                ? ActivatorUtilities.CreateInstance(Services, type)
                : TypeDescriptor.GetConverter(type).ConvertFromString(content);
        }

        private async Task<object> ParseAttributeValueAsync(Type type, string value, XElement element, DeserializeOperation operation)
        {
            string trimmedValue = value.Trim();
            Match match = Regex.Match(trimmedValue, @"\{.+\}");
            bool isMarkupExtension = match.Success;

            if (isMarkupExtension)
            {
                return await ParseMarkupExtensionAsync(trimmedValue, element, operation);
            }
            else
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                return converter.ConvertFromString(value);
            }
        }

        private async Task<object> ParseMarkupExtensionAsync(string value, XElement element, DeserializeOperation operation)
        {
            string[] markupExtensionString = value.Substring(1, value.Length - 2).Split(new[] { ',' }, 2);
            string[] markupExtensionClassWithParameters = markupExtensionString[0].Split(' ');
            string markupExtensionClass = markupExtensionClassWithParameters[0];

            GetNamespaceAndTypeName(markupExtensionClass, element, out string markupExtensionNamespace, out string markupExtensionName);

            Type markupExtensionType = GetTypeFromXmlName(markupExtensionNamespace, markupExtensionName);

            MarkupExtension markupExtension;

            if (markupExtensionClassWithParameters.Length > 1)
            {
                string[] markupExtensionParameters = markupExtensionClassWithParameters.AsSpan(1).ToArray();

                int parameterCount = markupExtensionParameters.Length;
                object[] parsedParameters = new object[markupExtensionParameters.Length];

                ParameterInfo[] constructorParameters = markupExtensionType.GetConstructors().First(c => c.GetParameters().Length == parameterCount).GetParameters();

                for (int i = 0; i < parameterCount; i++)
                {
                    parsedParameters[i] = TypeDescriptor.GetConverter(constructorParameters[i].ParameterType).ConvertFromString(markupExtensionParameters[i]);
                }

                markupExtension = (MarkupExtension)ActivatorUtilities.CreateInstance(Services, markupExtensionType, parsedParameters);
            }
            else
            {
                markupExtension = (MarkupExtension)ActivatorUtilities.CreateInstance(Services, markupExtensionType);
            }

            if (markupExtensionString.Length == 2)
            {
                string[] properties = markupExtensionString[1].Split(',');

                foreach (string propertyString in properties)
                {
                    string[] prop = propertyString.Split(new[] { '=' }, 2);
                    string propertyName = prop[0];
                    string propertyValue = prop[1];

                    PropertyInfo propertyInfo = markupExtensionType.GetProperty(propertyName);

                    DeserializeOperation newOperation = new DeserializeOperation(propertyInfo.PropertyType, operation.ParentReference!, operation.IdentifiableObjects);
                    object parsedValue = await ParseAttributeValueAsync(propertyInfo.PropertyType, propertyValue, element, operation);

                    markupExtensionType.GetProperty(propertyName).SetValue(markupExtension, parsedValue);
                }
            }

            IServiceProvider services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(operation)
                .AddSingleton(element)
                .BuildServiceProvider();

            return await markupExtension.ProvideValueAsync(services);
        }
    }
}
