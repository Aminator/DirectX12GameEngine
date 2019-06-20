using System;
using System.Collections;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        private async Task SerializeObjectAsync(string path, object asset, Type? storageType = null)
        {
            XElement root = SerializeObject(asset, storageType);

            using (Stream stream = await RootFolder.OpenStreamForWriteAsync(path, Windows.Storage.CreationCollisionOption.ReplaceExisting))
            {
                root.Save(stream);
            }

            Reference reference = new Reference(path, asset, true);
            SetAssetObject(reference);
        }

        private XElement SerializeObject(object asset, Type? storageType = null)
        {
            storageType ??= asset.GetType();

            XName elementName = GetElementName(storageType);
            XElement root = new XElement(elementName);

            if (loadedAssetReferences.TryGetValue(asset, out Reference reference))
            {
                root.Value = reference.Path.ToString();
                return root;
            }

            foreach (PropertyInfo propertyInfo in storageType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.IsDefined(typeof(XmlIgnoreAttribute)) || propertyInfo.IsSpecialName || propertyInfo.GetIndexParameters().Length > 0) continue;

                object? propertyValue = propertyInfo.GetValue(asset);

                if (propertyValue is null) continue;

                Type propertyType = propertyValue.GetType();

                // TODO: Put this logic into seperate serializers for each type.
                if (propertyType.IsPrimitive || propertyType == typeof(string) || propertyType == typeof(Vector3) || propertyType == typeof(Quaternion) || propertyType == typeof(Matrix4x4) || propertyType == typeof(Guid))
                {
                    if (propertyInfo.CanWrite)
                    {
                        object serializedValue = propertyValue switch
                        {
                            Vector3 vector => $"{vector.X},{vector.Y},{vector.Z}",
                            Quaternion quaternion => $"{quaternion.X},{quaternion.Y},{quaternion.Z},{quaternion.W}",
                            _ => propertyValue
                        };

                        XAttribute propertyAttribute = new XAttribute(propertyInfo.Name, serializedValue);
                        root.Add(propertyAttribute);
                    }
                }
                else
                {
                    XName propertySyntaxName = elementName + Type.Delimiter.ToString() + propertyInfo.Name;
                    XElement propertySyntax = new XElement(propertySyntaxName);

                    if (propertyValue is IList propertyCollection)
                    {
                        if (propertyCollection.Count > 0)
                        {
                            foreach (object childObject in propertyCollection)
                            {
                                XElement childElement = SerializeObject(childObject);
                                propertySyntax.Add(childElement);
                            }

                            root.Add(propertySyntax);
                        }
                    }
                    else if (propertyInfo.CanWrite)
                    {
                        XElement childElement = SerializeObject(propertyValue);
                        propertySyntax.Add(childElement);

                        root.Add(propertySyntax);
                    }
                }
            }

            if (asset is IList list)
            {
                foreach (object childObject in list)
                {
                    XElement childElement = SerializeObject(childObject);
                    root.Add(childElement);
                }
            }

            return root;
        }

        private static XName GetElementName(Type type)
        {
            XNamespace elementNamespace = type.Namespace;
            return elementNamespace + type.Name;
        }
    }
}
