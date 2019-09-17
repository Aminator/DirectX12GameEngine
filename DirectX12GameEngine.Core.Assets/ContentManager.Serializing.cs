using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Portable.Xaml;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        internal async Task SerializeAsync(string path, object obj)
        {
            using (Stream stream = await RootFolder.OpenStreamForWriteAsync(path + FileExtension, Windows.Storage.CreationCollisionOption.ReplaceExisting))
            {
                InternalXamlSchemaContext xamlSchemaContext = new InternalXamlSchemaContext(this);

                XmlWriter xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true });
                XamlXmlWriter writer = new XamlXmlWriter(xmlWriter, xamlSchemaContext);
                XamlObjectReader reader = new XamlObjectReader(obj, xamlSchemaContext);

                XamlServices.Transform(reader, writer);
            }

            Reference reference = new Reference(path, obj, true);
            AddReference(reference);
        }
    }
}
