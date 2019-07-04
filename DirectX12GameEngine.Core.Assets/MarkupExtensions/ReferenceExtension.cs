using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Core.Assets
{
    [DataContract(Namespace = "http://schemas.directx12gameengine.com/xaml/extensions")]
    public class ReferenceExtension : MarkupExtension
    {
        public ReferenceExtension()
        {
        }

        public ReferenceExtension(string id)
        {
            Id = id;
        }

        [DataMember]
        public string Id { get; set; }

        public override async Task<object> ProvideValueAsync(IServiceProvider services)
        {
            ContentManager.DeserializeOperation operation = services.GetRequiredService<ContentManager.DeserializeOperation>();

            Guid guid = Guid.Parse(Id);
            return await operation.IdentifiableObjects.GetValueAsync(guid);
        }
    }
}
