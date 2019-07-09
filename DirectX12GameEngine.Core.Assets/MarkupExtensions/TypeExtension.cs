using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DirectX12GameEngine.Core.Assets.MarkupExtensions
{
    public class TypeExtension : MarkupExtension
    {
        public TypeExtension()
        {
        }

        public TypeExtension(string typeName)
        {
            TypeName = typeName;
        }

        public string TypeName { get; set; }

        public override Task<object> ProvideValueAsync(IServiceProvider services)
        {
            XElement element = services.GetRequiredService<XElement>();

            ContentManager.GetNamespaceAndTypeName(TypeName, element, out string namespaceName, out string typeName);
            Type type = ContentManager.GetTypeFromXmlName(namespaceName, typeName);

            return Task.FromResult<object>(type);
        }
    }
}
