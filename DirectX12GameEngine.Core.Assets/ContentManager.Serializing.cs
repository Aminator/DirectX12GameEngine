using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
            XElement root = Serialize(obj, storageType, true);

            using (Stream stream = await RootFolder.OpenStreamForWriteAsync(path + FileExtension, Windows.Storage.CreationCollisionOption.ReplaceExisting))
            {
                root.Save(stream);
            }

            Reference reference = new Reference(path, obj, true);
            SetAsset(reference);
        }

        public XElement Serialize(object obj, Type? storageType = null, bool isRoot = false)
        {
            storageType ??= obj.GetType();

            ContractNamespaceAttribute contractNamespaceAttribute = storageType.Assembly.GetCustomAttributes<ContractNamespaceAttribute>().FirstOrDefault(c => c.ClrNamespace == storageType.Namespace);
            string assemblyName = storageType.Assembly.GetName().Name;

            string contractNamespace = contractNamespaceAttribute?.ContractNamespace ?? $"clr-namespace:{storageType.Namespace};assembly={assemblyName}";

            XNamespace x = "http://schemas.directx12gameengine.com/xaml/extensions";

            if (!isRoot && loadedAssetReferences.TryGetValue(obj, out Reference reference))
            {
                return new XElement(x + "AssetReference",
                    new XAttribute(x + "Path", reference.Path),
                    new XAttribute(x + "Type", $"{{x:Type {storageType.Name}}}"));
            }

            XName elementName = (XNamespace)contractNamespace + storageType.Name;
            XElement root = new XElement(elementName);

            if (isRoot)
            {
                root.Add(new XAttribute(XNamespace.Xmlns + "x", x.NamespaceName));
            }

            foreach (PropertyInfo propertyInfo in GetDataContractProperties(storageType, obj))
            {
                DataMemberAttribute? dataMember = propertyInfo.GetCustomAttribute<DataMemberAttribute>();
                XName propertyName = dataMember?.Name ?? propertyInfo.Name;

                object? propertyValue = propertyInfo.GetValue(obj);

                if (propertyValue is null) continue;

                TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyValue.GetType());

                if (loadedAssetReferences.TryGetValue(propertyValue, out Reference propertyReference))
                {
                    if (propertyInfo.CanWrite)
                    {
                        XAttribute propertyAttribute = new XAttribute(propertyName, $"{{x:AssetReference {propertyReference.Path}}}");
                        root.Add(propertyAttribute);
                    }
                }
                // TODO: Handle references better.
                else if (!(propertyValue is IList) && propertyValue is IIdentifiable identifiable)
                {
                    if (propertyInfo.CanWrite)
                    {
                        XAttribute propertyAttribute = new XAttribute(propertyName, $"{{x:Reference {identifiable.Id}}}");
                        root.Add(propertyAttribute);
                    }
                }
                else if (!(propertyValue is IList) && typeConverter.GetType() != typeof(TypeConverter) && typeConverter.CanConvertTo(typeof(string)))
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
