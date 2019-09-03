using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Core.Assets.MarkupExtensions
{
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

        public string? Path { get; set; }

        public Type? Type { get; set; }

        public override async Task<object> ProvideValueAsync(IServiceProvider services)
        {
            ContentManager contentManager = services.GetRequiredService<ContentManager>();
            ContentManager.DeserializeOperation operation = services.GetRequiredService<ContentManager.DeserializeOperation>();

            return await contentManager.DeserializeAsync(Path!, Type ?? operation.Type, operation.ParentReference, null);
        }
    }
}
