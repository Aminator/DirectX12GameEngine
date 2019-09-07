using System;
using System.Collections.Generic;
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

            Reference? reference = FindDeserializedObject(initialPath, type);

            if (reference is null || reference.Object != instance)
            {
                throw new InvalidOperationException();
            }

            HashSet<Reference>? references = reference.References;
            reference.References = new HashSet<Reference>();
            reference.IsDeserialized = false;

            object asset = await DeserializeAsync(newPath, type, null, instance);

            foreach (Reference childReference in references)
            {
                DecrementReference(childReference, false);
            }

            return asset;
        }

        internal async Task<object> DeserializeAsync(string path, Type type, object? parent, object? instance)
        {
            Reference? parentReference = null;

            if (parent != null)
            {
                loadedAssetReferences.TryGetValue(parent, out parentReference);
            }

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

            if (!await ExistsAsync(path).ConfigureAwait(false))
            {
                throw new FileNotFoundException();
            }

            object? rootObjectInstance = reference?.Object ?? instance;
            TaskCompletionSource<bool> hasRootObject = new TaskCompletionSource<bool>();

            InternalXamlSchemaContext xamlSchemaContext = new InternalXamlSchemaContext(this);

            XamlObjectWriter writer = new XamlObjectWriter(xamlSchemaContext, new XamlObjectWriterSettings
            {
                RootObjectInstance = rootObjectInstance,
                BeforePropertiesHandler = (s, e) =>
                {
                    if (hasRootObject.Task.IsCompleted) return;

                    rootObjectInstance = e.Instance;

                    if (rootObjectInstance is null) throw new InvalidOperationException();

                    object? result = rootObjectInstance;

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

                    reference = new Reference(path, result, isRoot);
                    AddReference(reference);

                    reference.IsDeserialized = true;

                    hasRootObject.TrySetResult(true);
                }
            });

            Task<object> transformTask = Task.Run(async () =>
            {
                using Stream stream = await RootFolder.OpenStreamForReadAsync(path + FileExtension).ConfigureAwait(false);
                XamlXmlReader reader = new XamlXmlReader(stream, xamlSchemaContext);

                XamlServices.Transform(reader, writer);

                if (reference?.Object is null) throw new InvalidOperationException();

                if (rootObjectInstance != reference.Object)
                {
                    if (!(rootObjectInstance is Asset asset)) throw new InvalidOperationException();

                    await asset.CreateAssetAsync(reference.Object, Services).ConfigureAwait(false);
                }

                parentReference?.References.Add(reference);

                return reference.Object;
            });

            await hasRootObject.Task.ConfigureAwait(false);

            if (reference?.Object is null) throw new InvalidOperationException();

            await transformTask.ConfigureAwait(false);

            return reference.Object;
        }
    }
}
