using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            Reference? reference = FindDeserializedObject(initialPath, type);

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
            bool isRoot = parentReference is null;

            // Check if reference exists and immediately return.

            if (referenceToReload is null)
            {
                Reference? foundReference = FindDeserializedObject(path, type);

                if (foundReference != null)
                {
                    if (isRoot || parentReference!.References.Add(foundReference))
                    {
                        IncrementReference(foundReference, isRoot);
                    }

                    if (foundReference.DeserializationTask is null) throw new InvalidOperationException();

                    await foundReference.DeserializationTask.ConfigureAwait(false);

                    return foundReference.Object;
                }
            }

            // Reference not found, so deserialize asset.

            if (!await ExistsAsync(path).ConfigureAwait(false))
            {
                throw new FileNotFoundException();
            }

            Stream stream = await RootFolder.OpenStreamForReadAsync(path + FileExtension).ConfigureAwait(false);
            Type rootObjectType = GetRootObjectType(stream);

            object rootObjectInstance = referenceToReload != null && referenceToReload.Object.GetType().IsInstanceOfType(rootObjectType)
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

            Reference reference = referenceToReload ?? new Reference(path, result, isRoot);

            if (referenceToReload is null)
            {
                AddReference(reference);
            }

            parentReference?.References.Add(reference);

            // Deserialization

            Task deserializationTask = Task.Run(async () =>
            {
                parentReference?.ToString();

                InternalXamlSchemaContext xamlSchemaContext = new InternalXamlSchemaContext(this, reference);

                XamlObjectWriter writer = new XamlObjectWriter(xamlSchemaContext, new XamlObjectWriterSettings
                {
                    RootObjectInstance = rootObjectInstance
                });

                XamlXmlReader reader = new XamlXmlReader(stream, xamlSchemaContext);

                XamlServices.Transform(reader, writer);

                stream.Dispose();

                await Task.WhenAll(reference.References.Select(r => r.DeserializationTask ?? throw new InvalidOperationException())).ConfigureAwait(false);

                if (rootObjectInstance != reference.Object)
                {
                    if (!(rootObjectInstance is Asset asset)) throw new InvalidOperationException();

                    await asset.CreateAssetAsync(reference.Object, Services).ConfigureAwait(false);
                }
            });

            reference.DeserializationTask = deserializationTask;

            await reference.DeserializationTask.ConfigureAwait(false);

            return reference.Object;
        }
    }
}
