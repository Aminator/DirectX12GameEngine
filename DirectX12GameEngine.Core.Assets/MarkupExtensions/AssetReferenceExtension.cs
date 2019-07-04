using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Core.Assets
{
    [DataContract(Namespace = "http://schemas.directx12gameengine.com/xaml/extensions")]
    public class AssetReferenceExtension : MarkupExtension
    {
        public AssetReferenceExtension()
        {
        }

        public AssetReferenceExtension(string path)
        {
            Path = path;
        }

        public AssetReferenceExtension(string path, Type type) : this(path)
        {
            Type = type;
        }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public Type? Type { get; set; }

        public override async Task<object> ProvideValueAsync(IServiceProvider services)
        {
            ContentManager contentManager = services.GetRequiredService<ContentManager>();
            ContentManager.DeserializeOperation operation = services.GetRequiredService<ContentManager.DeserializeOperation>();

            return await contentManager.DeserializeAsync(Path, Type ?? operation.Type, operation.ParentReference, null);
        }
    }
}
