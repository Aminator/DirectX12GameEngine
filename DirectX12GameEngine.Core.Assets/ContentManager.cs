using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Portable.Xaml;
using Windows.Storage;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager : IContentManager
    {
        private readonly AsyncLock asyncLock = new AsyncLock();

        private readonly Dictionary<string, Reference> loadedAssetPaths = new Dictionary<string, Reference>();
        private readonly Dictionary<object, Reference> loadedAssetReferences = new Dictionary<object, Reference>();

        static ContentManager()
        {
            AddTypeConverters(typeof(Vector3Converter), typeof(Vector4Converter), typeof(QuaternionConverter), typeof(Matrix4x4Converter));
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
            return await DeserializeAsync(path, type, null, null);
        }

        public async Task<bool> ReloadAsync(object asset, string? newPath = null)
        {
            if (!loadedAssetReferences.TryGetValue(asset, out Reference reference))
            {
                return false;
            }

            string path = newPath ?? reference.Path;

            await DeserializeExistingObjectAsync(reference.Path, path, asset);

            if (path != reference.Path)
            {
                loadedAssetPaths.Remove(reference.Path);
            }

            return true;
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
            if (!loadedAssetPaths.TryGetValue(path, out Reference reference))
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

        public static IEnumerable<PropertyInfo> GetDataContractProperties(Type type, object obj)
        {
            bool isDataContractPresent = type.IsDefined(typeof(DataContractAttribute));

            var properties = !isDataContractPresent
                ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance).AsEnumerable()
                : type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.IsDefined(typeof(DataMemberAttribute))).OrderBy(p => p.GetCustomAttribute<DataMemberAttribute>().Order);

            properties = properties.Where(p => !p.IsDefined(typeof(IgnoreDataMemberAttribute)) && !p.IsSpecialName && !(p.GetIndexParameters().Length > 0));

            return properties.Where(p => p.CanRead && p.GetMethod.IsPublic).Where(p => (p.CanWrite && p.SetMethod.IsPublic) || p.GetValue(obj) is ICollection);
        }

        public static Type GetRootObjectType(Stream stream)
        {
            XamlXmlReader reader = new XamlXmlReader(stream);

            while (reader.NodeType != XamlNodeType.StartObject)
            {
                reader.Read();
            }

            Type type = reader.Type.UnderlyingType;

            stream.Seek(0, SeekOrigin.Begin);

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
        }
    }
}
