using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nito.AsyncEx;
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
            var types = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.ExportedTypes.Where(t => !(t.IsAbstract && t.IsSealed)));

            foreach (Type type in types)
            {
                AddType(type);
            }

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        public ContentManager(IServiceProvider services)
        {
            Services = services;
        }

        public ContentManager(IServiceProvider services, StorageFolder rootFolder)
        {
            Services = services;
            RootFolder = rootFolder;
        }

        public string FileExtension = ".xml";

        public string RootPath => RootFolder.Path;

        public StorageFolder RootFolder { get; set; }

        public IServiceProvider Services { get; }

        internal static Dictionary<string, Dictionary<string, Type>> LoadedTypes { get; } = new Dictionary<string, Dictionary<string, Type>>();

        public async Task<bool> ExistsAsync(string path)
        {
            return await RootFolder.TryGetItemAsync(path + FileExtension) != null;
        }

        public T Get<T>(string path) where T : class?
        {
            object? asset = Get(typeof(T), path);

            if (asset != null)
            {
                return (T)asset;
            }

            return null!;
        }

        public object? Get(Type type, string path)
        {
            return FindDeserializedObject(path, type)?.Object;
        }

        public async Task<T> LoadAsync<T>(string path) where T : class
        {
            return (T)await LoadAsync(typeof(T), path);
        }

        public async Task<object> LoadAsync(Type type, string path)
        {
            using (await asyncLock.LockAsync())
            {
                return await DeserializeAsync(path, path, type, null);
            }
        }

        public async Task<bool> ReloadAsync(object asset, string? newPath = null)
        {
            using (await asyncLock.LockAsync())
            {
                if (!loadedAssetReferences.TryGetValue(asset, out Reference reference))
                {
                    return false;
                }

                string path = newPath ?? reference.Path;

                await DeserializeAsync(reference.Path, path, asset.GetType(), asset);

                if (path != reference.Path)
                {
                    loadedAssetPaths.Remove(reference.Path);
                }

                return true;
            }
        }

        public async Task SaveAsync(string path, object asset, Type? storageType = null)
        {
            using (await asyncLock.LockAsync())
            {
                await SerializeAsync(path, asset, storageType);
            }
        }

        public void Unload(object asset)
        {
            using (asyncLock.Lock())
            {
                if (!loadedAssetReferences.TryGetValue(asset, out Reference reference))
                {
                    throw new InvalidOperationException("Content is not loaded.");
                }

                DecrementReference(reference, true);
            }
        }

        public void Unload(string path)
        {
            using (asyncLock.Lock())
            {
                if (!loadedAssetPaths.TryGetValue(path, out Reference reference))
                {
                    throw new InvalidOperationException("Content is not loaded.");
                }

                DecrementReference(reference, true);
            }
        }

        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (!args.LoadedAssembly.IsDynamic)
            {
                var types = args.LoadedAssembly.ExportedTypes.Where(t => !(t.IsAbstract && t.IsSealed));

                foreach (Type type in types)
                {
                    AddType(type);
                }
            }
        }

        private static void AddType(Type type)
        {
            GetDataContractName(type, out string dataContractNamespace, out string dataContractName);

            if (!LoadedTypes.TryGetValue(dataContractNamespace, out Dictionary<string, Type> types))
            {
                types = new Dictionary<string, Type>();
                LoadedTypes.Add(dataContractNamespace, types);
            }

            if (!types.ContainsKey(dataContractName))
            {
                types.Add(dataContractName, type);

                GlobalTypeConverterAttribute? converterAttribute = type.GetCustomAttribute<GlobalTypeConverterAttribute>();

                if (converterAttribute != null)
                {
                    TypeDescriptor.AddAttributes(converterAttribute.Type, new TypeConverterAttribute(type));
                }
            }
        }

        private static void GetDataContractName(Type type, out string dataContractNamespace, out string dataContractName)
        {
            DataContractAttribute? dataContract = type.GetCustomAttribute<DataContractAttribute>();
            ContractNamespaceAttribute? contractNamespace = type.Assembly.GetCustomAttribute<ContractNamespaceAttribute>();

            dataContractNamespace = dataContract?.Namespace ?? contractNamespace?.ContractNamespace ?? "using:" + type.Namespace;
            dataContractName = dataContract?.Name ?? type.Name;
        }
    }
}
