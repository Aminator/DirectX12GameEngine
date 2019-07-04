using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Core.Assets.MarkupExtensions
{
    [DataContract(Namespace = "http://schemas.directx12gameengine.com/xaml/extensions")]
    public class TypeExtension : MarkupExtension
    {
        public TypeExtension()
        {
        }

        public TypeExtension(string typeName)
        {
            TypeName = typeName;
        }

        [DataMember]
        public string TypeName { get; set; }

        public override Task<object> ProvideValueAsync(IServiceProvider services)
        {
            XElement element = services.GetRequiredService<XElement>();

            ContentManager.GetNamespaceAndTypeName(element, TypeName, out string namespaceName, out string typeName);
            Type type = ContentManager.LoadedTypes[namespaceName][typeName];

            return Task.FromResult<object>(type);
        }
    }
}
