using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        internal async Task SerializeAsync(string path, object obj, Type? storageType = null)
        {
            XElement root = Serialize(obj, storageType);

            using (Stream stream = await RootFolder.OpenStreamForWriteAsync(path + FileExtension, Windows.Storage.CreationCollisionOption.ReplaceExisting))
            {
                root.Save(stream);
            }

            Reference reference = new Reference(path, obj, true);
            SetAsset(reference);
        }

        public XElement Serialize(object obj, Type? storageType = null)
        {
            storageType ??= obj.GetType();

            GetDataContractName(storageType, out string dataContractNamespace, out string dataContractName);

            XName typeExtension = (XNamespace)"http://schemas.directx12gameengine.com/xaml/extensions" + "Type";
            XName assetReferenceExtension = (XNamespace)"http://schemas.directx12gameengine.com/xaml/extensions" + "AssetReference";

            if (loadedAssetReferences.TryGetValue(obj, out Reference reference))
            {
                return new XElement(assetReferenceExtension,
                    new XAttribute("Path", reference.Path),
                    new XAttribute("Type", $"{{{typeExtension} {storageType.Name}}}"));
            }

            XName elementName = (XNamespace)dataContractNamespace + dataContractName;
            XElement root = new XElement(elementName);

            bool isDataContractPresent = storageType.IsDefined(typeof(DataContractAttribute));

            var properties = !isDataContractPresent
                ? storageType.GetProperties(BindingFlags.Public | BindingFlags.Instance).AsEnumerable()
                : storageType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.IsDefined(typeof(DataMemberAttribute))).OrderBy(p => p.GetCustomAttribute<DataMemberAttribute>().Order);

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.IsDefined(typeof(IgnoreDataMemberAttribute)) || propertyInfo.IsSpecialName || propertyInfo.GetIndexParameters().Length > 0) continue;

                DataMemberAttribute? dataMember = propertyInfo.GetCustomAttribute<DataMemberAttribute>();
                XName propertyName = dataMember?.Name ?? propertyInfo.Name;

                object? propertyValue = propertyInfo.GetValue(obj);

                if (propertyValue is null) continue;

                Type propertyType = propertyValue.GetType();

                TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyType);

                if (loadedAssetReferences.TryGetValue(propertyValue, out Reference propertyReference))
                {
                    XAttribute propertyAttribute = new XAttribute(propertyName, $"{{{assetReferenceExtension} {propertyReference.Path}}}");
                    root.Add(propertyAttribute);
                }
                else if (typeConverter.CanConvertTo(typeof(string)))
                {
                    if (propertyInfo.CanWrite)
                    {
                        object serializedValue = typeConverter.ConvertToString(propertyValue);

                        XAttribute propertyAttribute = new XAttribute(propertyName, serializedValue);
                        root.Add(propertyAttribute);
                    }
                }
                else
                {
                    XName propertySyntaxName = elementName + Type.Delimiter.ToString() + propertyName;
                    XElement propertySyntax = new XElement(propertySyntaxName);

                    if (propertyValue is IList propertyCollection)
                    {
                        if (propertyCollection.Count > 0)
                        {
                            foreach (object childObject in propertyCollection)
                            {
                                XElement childElement = Serialize(childObject);
                                propertySyntax.Add(childElement);
                            }

                            root.Add(propertySyntax);
                        }
                    }
                    else if (propertyInfo.CanWrite)
                    {
                        XElement childElement = Serialize(propertyValue);
                        propertySyntax.Add(childElement);

                        root.Add(propertySyntax);
                    }
                }
            }

            if (obj is IList list)
            {
                foreach (object childObject in list)
                {
                    XElement childElement = Serialize(childObject);
                    root.Add(childElement);
                }
            }

            return root;
        }
    }
}
