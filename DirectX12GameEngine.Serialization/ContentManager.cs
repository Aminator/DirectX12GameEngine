using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Portable.Xaml;

namespace DirectX12GameEngine.Serialization
{
    public partial class ContentManager : IContentManager
    {
        private readonly Dictionary<string, Reference> loadedAssetPaths = new Dictionary<string, Reference>();
        private readonly Dictionary<object, Reference> loadedAssetReferences = new Dictionary<object, Reference>();

        static ContentManager()
        {
            AddTypeConverters(typeof(Vector2Converter), typeof(Vector3Converter), typeof(Vector4Converter), typeof(QuaternionConverter), typeof(Matrix4x4Converter));
        }

        public ContentManager(IServiceProvider services, IFileProvider fileProvider)
        {
            Services = services;
            FileProvider = fileProvider;
        }

        public IServiceProvider Services { get; }

        public IFileProvider FileProvider { get; }

        public string FileExtension { get; set; } = ".xaml";

        public Task<bool> ExistsAsync(string path)
        {
            return FileProvider.ExistsAsync(path + FileExtension);
        }

        public async Task<T> GetAsync<T>(string path) where T : class?
        {
            object? asset = await GetAsync(typeof(T), path);

            if (asset != null)
            {
                return (T)asset;
            }

            return null!;
        }

        public async Task<object?> GetAsync(Type type, string path)
        {
            Reference? reference = await FindDeserializedReferenceAsync(path, type);

            return reference?.Object ?? null;
        }

        public bool IsLoaded(string path)
        {
            return loadedAssetPaths.ContainsKey(path);
        }

        public async Task<T> LoadAsync<T>(string path) where T : class
        {
            return (T)await LoadAsync(typeof(T), path);
        }

        public async Task<object> LoadAsync(Type type, string path)
        {
            return await DeserializeAsync(path, type, null);
        }

        public async Task SaveAsync(string path, object asset)
        {
            await SerializeAsync(path, asset);
        }

        public bool TryGetAssetPath(object asset, out string? path)
        {
            if (loadedAssetReferences.TryGetValue(asset, out Reference reference))
            {
                path = reference.Path;
                return true;
            }

            path = null;
            return false;
        }

        public void Unload(object asset)
        {
            if (!loadedAssetReferences.TryGetValue(asset, out Reference reference))
            {
                throw new InvalidOperationException("Content is not loaded.");
            }

            DecrementReference(reference, true);
        }

        public void Unload(string path)
        {
            if (!loadedAssetPaths.TryGetValue(path, out Reference reference) || reference.Object is null)
            {
                throw new InvalidOperationException("Content is not loaded.");
            }

            DecrementReference(reference, true);
        }

        public static void AddTypeConverters(params Type[] typeConverters)
        {
            foreach (Type typeConverter in typeConverters)
            {
                GlobalTypeConverterAttribute converterAttribute = typeConverter.GetCustomAttribute<GlobalTypeConverterAttribute>();
                TypeDescriptor.AddAttributes(converterAttribute.Type, new TypeConverterAttribute(typeConverter));
            }
        }

        public static IEnumerable<PropertyInfo> GetDataContractProperties(Type type)
        {
            bool isDataContractPresent = type.IsDefined(typeof(DataContractAttribute));

            var properties = !isDataContractPresent
                ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance).AsEnumerable()
                : type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.IsDefined(typeof(DataMemberAttribute))).OrderBy(p => p.GetCustomAttribute<DataMemberAttribute>().Order);

            properties = properties.Where(p => !p.IsDefined(typeof(IgnoreDataMemberAttribute)) && !p.IsSpecialName && !(p.GetIndexParameters().Length > 0));

            return properties.Where(p => p.CanRead && p.GetMethod.IsPublic);
        }

        public static Type? GetRootObjectType(Stream stream)
        {
            XamlXmlReader reader = new XamlXmlReader(stream);

            while (reader.NodeType != XamlNodeType.StartObject)
            {
                reader.Read();
            }

            Type type = reader.Type.UnderlyingType;

            stream.Position = 0;

            return type;
        }

        internal class InternalXamlSchemaContext : XamlSchemaContext
        {
            public InternalXamlSchemaContext(ContentManager contentManager, Reference? parentReference = null)
            {
                ContentManager = contentManager;
                ParentReference = parentReference;
            }

            public ContentManager ContentManager { get; }

            public Reference? ParentReference { get; }

            protected override ICustomAttributeProvider GetCustomAttributeProvider(Type type)
            {
                return new TypeAttributeProvider(type);
            }

            protected override ICustomAttributeProvider GetCustomAttributeProvider(MemberInfo member)
            {
                return new MemberAttributeProvider(member);
            }
        }

        private class TypeAttributeProvider : ICustomAttributeProvider
        {
            private readonly Type type;

            public TypeAttributeProvider(Type type)
            {
                this.type = type;
            }

            public object[] GetCustomAttributes(bool inherit)
            {
                return type.GetCustomAttributes(inherit);
            }

            public object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                return type.GetCustomAttributes(attributeType, inherit);
            }

            public bool IsDefined(Type attributeType, bool inherit)
            {
                return type.IsDefined(attributeType, inherit);
            }
        }

        private class MemberAttributeProvider : ICustomAttributeProvider
        {
            private readonly MemberInfo memberInfo;

            public MemberAttributeProvider(MemberInfo memberInfo)
            {
                this.memberInfo = memberInfo;
            }

            public object[] GetCustomAttributes(bool inherit)
            {
                var attributes = memberInfo.GetCustomAttributes(inherit);
                return GetAdditionalAttributes(attributes, inherit);
            }

            public object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                var attributes = memberInfo.GetCustomAttributes(attributeType, inherit);
                return GetAdditionalAttributes(attributes, inherit);
            }

            public bool IsDefined(Type attributeType, bool inherit)
            {
                return memberInfo.IsDefined(attributeType, inherit);
            }

            private object[] GetAdditionalAttributes(object[] attributes, bool inherit)
            {
                if (!memberInfo.IsDefined(typeof(DesignerSerializationVisibilityAttribute), inherit) && memberInfo.IsDefined(typeof(IgnoreDataMemberAttribute), inherit))
                {
                    return attributes.Concat(new[] { new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden) }).ToArray();
                }

                return attributes;
            }
        }
    }
}
