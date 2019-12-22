using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Portable.Xaml;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        internal async Task<object> DeserializeExistingObjectAsync(string initialPath, string newPath, object instance)
        {
            Type type = instance.GetType();

            Reference? reference = await FindDeserializedReferenceAsync(initialPath, type);

            if (reference is null || reference.Object != instance)
            {
                throw new InvalidOperationException();
            }

            HashSet<Reference>? references = reference.References;
            reference.References = new HashSet<Reference>();

            object asset = await DeserializeAsync(newPath, type, null, reference);

            foreach (Reference childReference in references)
            {
                DecrementReference(childReference, false);
            }

            return asset;
        }

        internal async Task<object> DeserializeAsync(string path, Type type, Reference? parentReference, Reference? referenceToReload)
        {
            Reference reference;

            // Check if reference exists and immediately return.

            if (referenceToReload is null)
            {
                Reference? foundReference = await FindDeserializedReferenceAsync(path, type);

                if (foundReference != null)
                {
                    if (parentReference is null || parentReference.References.Add(foundReference))
                    {
                        IncrementReference(foundReference, parentReference is null);
                    }

                    return foundReference.Object;
                }
            }

            // Reference not found, so deserialize asset.

            if (!await ExistsAsync(path))
            {
                throw new FileNotFoundException();
            }

            using Stream stream = await FileProvider.OpenStreamAsync(path + FileExtension, FileMode.Open, FileAccess.Read);
            Type? rootObjectType = GetRootObjectType(stream);

            if (rootObjectType is null)
            {
                throw new InvalidOperationException();
            }

            object rootObjectInstance = referenceToReload != null && referenceToReload.Object.GetType().IsAssignableFrom(rootObjectType)
                ? referenceToReload.Object : Activator.CreateInstance(rootObjectType);

            object result = rootObjectInstance;

            if (!type.IsInstanceOfType(rootObjectInstance) || (type == typeof(object) && rootObjectInstance is Asset))
            {
                if (!(rootObjectInstance is Asset)) throw new InvalidOperationException();

                AssetContentTypeAttribute? contentType = rootObjectInstance.GetType().GetCustomAttribute<AssetContentTypeAttribute>();

                if (type == typeof(object) && contentType != null)
                {
                    type = contentType.ContentType;
                }

                result = Activator.CreateInstance(type);
            }

            reference = referenceToReload ?? new Reference(path, result, parentReference is null);

            if (referenceToReload != null)
            {
                object instance = referenceToReload.Object;

                ClearCollections(instance);
            }

            reference.DeserializationTask = Task.Run(async () => await DeserializeAsync(stream, rootObjectInstance, reference));

            if (referenceToReload is null)
            {
                AddReference(reference);
            }

            parentReference?.References.Add(reference);

            await reference.DeserializationTask;

            return reference.Object;
        }

        private static void ClearCollections(object instance)
        {
            foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
            {
                var visibilityAttribute = propertyInfo.GetCustomAttribute<DesignerSerializationVisibilityAttribute>();
                bool isVisible = visibilityAttribute is null || visibilityAttribute.Visibility != DesignerSerializationVisibility.Hidden;

                if (propertyInfo.CanRead && isVisible)
                {
                    object value = propertyInfo.GetValue(instance);

                    if (value is IList list)
                    {
                        while (list.Count > 0)
                        {
                            list.RemoveAt(list.Count - 1);
                        }
                    }
                    else if (value is IDictionary dictionary)
                    {
                        foreach (object key in dictionary.Keys)
                        {
                            dictionary.Remove(key);
                        }
                    }
                }
            }
        }

        private async Task DeserializeAsync(Stream stream, object rootObjectInstance, Reference reference)
        {
            InternalXamlSchemaContext xamlSchemaContext = new InternalXamlSchemaContext(this, reference);

            XamlObjectWriter writer = new XamlObjectWriter(xamlSchemaContext, new XamlObjectWriterSettings
            {
                RootObjectInstance = rootObjectInstance
            });

            XamlXmlReader reader = new XamlXmlReader(stream, xamlSchemaContext);

            XamlServices.Transform(reader, writer);

            if (rootObjectInstance != reference.Object)
            {
                if (!(rootObjectInstance is Asset asset)) throw new InvalidOperationException();

                await asset.CreateAssetAsync(reference.Object, Services);
            }
        }
    }
}
