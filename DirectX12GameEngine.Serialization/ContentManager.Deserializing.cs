using System;
using System.IO;
using System.Threading.Tasks;
using Portable.Xaml;

namespace DirectX12GameEngine.Serialization
{
    public partial class ContentManager
    {
        internal async Task<object> DeserializeAsync(string path, Type type, Reference? parentReference)
        {
            Reference reference;

            // Check if reference exists and immediately return.

            Reference? foundReference = await FindDeserializedReferenceAsync(path, type);

            if (foundReference != null)
            {
                if (parentReference is null || parentReference.References.Add(foundReference))
                {
                    IncrementReference(foundReference, parentReference is null);
                }

                if (foundReference.Object is null) throw new InvalidOperationException();

                return foundReference.Object;
            }

            // Reference not found, so deserialize asset.

            if (!await ExistsAsync(path))
            {
                throw new FileNotFoundException($"The asset file {path} could not be found.", path);
            }

            using Stream stream = await FileProvider.OpenStreamAsync(path + FileExtension, FileMode.Open, FileAccess.Read);
            Type? rootObjectType = GetRootObjectType(stream);

            if (rootObjectType is null)
            {
                throw new InvalidOperationException();
            }

            reference = new Reference(path, parentReference is null);
            reference.DeserializationTask = Task.Run(() => DeserializeAsync(stream, type, reference));

            AddReference(reference);

            parentReference?.References.Add(reference);

            await reference.DeserializationTask;

            if (reference.Object is null) throw new InvalidOperationException();

            return reference.Object;
        }

        private async Task DeserializeAsync(Stream stream, Type type, Reference reference)
        {
            InternalXamlSchemaContext xamlSchemaContext = new InternalXamlSchemaContext(this, reference);

            XamlObjectWriter writer = new XamlObjectWriter(xamlSchemaContext);
            XamlXmlReader reader = new XamlXmlReader(stream, xamlSchemaContext);

            XamlServices.Transform(reader, writer);

            if (type == typeof(object) && writer.Result is Asset || !type.IsInstanceOfType(writer.Result))
            {
                if (!(writer.Result is Asset asset)) throw new InvalidOperationException();

                reference.Object = await asset.CreateAssetAsync(Services);
            }
            else
            {
                reference.Object = writer.Result;
            }

            loadedAssetReferences[reference.Object] = reference;
        }
    }
}
