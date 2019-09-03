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

            if (!isRoot && loadedAssetReferences.TryGetValue(obj, out Reference reference))
            {
                return new XElement(ExtensionsNamespace + "AssetReference",
                    new XAttribute(ExtensionsNamespace + "Path", reference.Path),
                    new XAttribute(ExtensionsNamespace + "Type", $"{{x:Type {storageType.Name}}}"));
            }

            XName elementName = (XNamespace)contractNamespace + storageType.Name;
            XElement root = new XElement(elementName);

            if (isRoot)
            {
                root.Add(new XAttribute(XNamespace.Xmlns + "x", ExtensionsNamespace.NamespaceName));
            }

            TypeConverter typeConverter = TypeDescriptor.GetConverter(storageType);

            if (typeConverter.GetType() != typeof(TypeConverter) && typeConverter.CanConvertTo(typeof(string)))
            {
                string serializedValue = typeConverter.ConvertToString(obj);
                root.Value = serializedValue;
            }

            foreach (PropertyInfo propertyInfo in GetDataContractProperties(storageType, obj))
            {
                DataMemberAttribute? dataMember = propertyInfo.GetCustomAttribute<DataMemberAttribute>();
                XName propertyName = dataMember?.Name ?? propertyInfo.Name;
                object? propertyValue = propertyInfo.GetValue(obj);

                WriteProperty(propertyName, propertyValue, root, propertyInfo.CanWrite);
            }

            if (obj is ICollection collection)
            {
                WriteCollection(root, collection);
            }

            return root;
        }

        private void WriteProperty(XName propertyName, object propertyValue, XElement root, bool canWrite)
        {
            if (propertyValue is null) return;

            TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyValue.GetType());

            XName propertySyntaxName = root.Name + Type.Delimiter.ToString() + propertyName;
            XElement propertySyntax = new XElement(propertySyntaxName);

            if (canWrite)
            {
                if (loadedAssetReferences.TryGetValue(propertyValue, out Reference propertyReference))
                {
                    XAttribute propertyAttribute = new XAttribute(propertyName, $"{{x:AssetReference {propertyReference.Path}}}");
                    root.Add(propertyAttribute);
                }
                // TODO: Handle references better.
                else if (propertyValue is IIdentifiable identifiable)
                {
                    XAttribute propertyAttribute = new XAttribute(propertyName, $"{{x:Reference {identifiable.Id}}}");
                    root.Add(propertyAttribute);
                }
                else if (typeConverter.GetType() != typeof(TypeConverter) && typeConverter.GetType() != typeof(CollectionConverter) && typeConverter.CanConvertTo(typeof(string)))
                {
                    string serializedValue = typeConverter.ConvertToString(propertyValue);

                    XAttribute propertyAttribute = new XAttribute(propertyName, serializedValue);
                    root.Add(propertyAttribute);
                }
                else
                {
                    XElement childElement = Serialize(propertyValue);
                    propertySyntax.Add(childElement);

                    root.Add(propertySyntax);
                }
            }
            else if (propertyValue is ICollection propertyCollection)
            {
                if (propertyCollection.Count > 0)
                {
                    WriteCollection(propertySyntax, propertyCollection);

                    root.Add(propertySyntax);
                }
            }
        }

        private void WriteCollection(XElement root, ICollection collection)
        {
            if (collection is IDictionary dictionary)
            {
                foreach (DictionaryEntry childObject in dictionary)
                {
                    XElement childElement = Serialize(childObject.Value);
                    WriteProperty(ExtensionsNamespace + "Key", childObject.Key, childElement, true);
                    root.Add(childElement);
                }
            }
            else
            {
                foreach (object childObject in collection)
                {
                    XElement childElement = Serialize(childObject);
                    root.Add(childElement);
                }
            }
        }
    }
}
