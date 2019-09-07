using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Portable.Xaml;
using Portable.Xaml.Markup;

namespace DirectX12GameEngine.Core.Assets
{
    public class AssetReferenceTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string path)
            {
                IValueSerializerContext valueSerializerContext = (IValueSerializerContext)context;
                IXamlSchemaContextProvider xamlSchemaContextProvider = valueSerializerContext.GetRequiredService<IXamlSchemaContextProvider>();

                if (xamlSchemaContextProvider.SchemaContext is ContentManager.InternalXamlSchemaContext xamlSchemaContext)
                {
                    IDestinationTypeProvider destinationTypeProvider = valueSerializerContext.GetRequiredService<IDestinationTypeProvider>();
                    IRootObjectProvider rootObjectProvider = valueSerializerContext.GetRequiredService<IRootObjectProvider>();

                    Type type = destinationTypeProvider.GetDestinationType();
                    object rootObject = rootObjectProvider.RootObject;

                    return AsyncHelper.RunSync(() => xamlSchemaContext.ContentManager.DeserializeAsync(path, type, rootObject, null));
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            IValueSerializerContext valueSerializerContext = (IValueSerializerContext)context;
            IXamlSchemaContextProvider xamlSchemaContextProvider = valueSerializerContext.GetRequiredService<IXamlSchemaContextProvider>();

            if (xamlSchemaContextProvider.SchemaContext is ContentManager.InternalXamlSchemaContext xamlSchemaContext)
            {
                if (xamlSchemaContext.ContentManager.TryGetAssetPath(value, out string? path))
                {
                    return path!;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
