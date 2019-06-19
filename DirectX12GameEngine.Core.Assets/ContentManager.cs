using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectX12GameEngine.Core.Assets
{
    public sealed partial class ContentManager : IContentManager
    {
        private readonly AsyncDictionary<Guid, IIdentifiable> identifiableObjects = new AsyncDictionary<Guid, IIdentifiable>();
        private readonly Dictionary<string, Reference> loadedAssetPaths = new Dictionary<string, Reference>();
        private readonly Dictionary<object, Reference> loadedAssetReferences = new Dictionary<object, Reference>();

        static ContentManager()
        {
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

            var types = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.GetExportedTypes().Where(t => !(t.IsAbstract && t.IsSealed)));

            foreach (Type type in types)
            {
                if (!LoadedTypes.ContainsKey(type.FullName))
                {
                    LoadedTypes.Add(type.FullName, type);
                }
            }
        }

        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (!args.LoadedAssembly.IsDynamic)
            {
                var types = args.LoadedAssembly.GetExportedTypes().Where(t => !(t.IsAbstract && t.IsSealed));

                foreach (Type type in types)
                {
                    if (!LoadedTypes.ContainsKey(type.FullName))
                    {
                        LoadedTypes.Add(type.FullName, type);
                    }
                }
            }
        }

        public ContentManager(IServiceProvider services)
        {
            Services = services;

            try
            {
                RootFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            }
            catch
            {
            }
        }

        public ContentManager(IServiceProvider services, StorageFolder rootFolder)
        {
            Services = services;
            RootFolder = rootFolder;
        }

        public static Dictionary<string, Type> LoadedTypes { get; } = new Dictionary<string, Type>();

        public string RootPath => RootFolder.Path;

        public StorageFolder RootFolder { get; set; }

        public IServiceProvider Services { get; }

        public async Task<bool> ExistsAsync(string path)
        {
            return await RootFolder.TryGetItemAsync(path) != null;
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

        public async Task<T> LoadAsync<T>(string path)
        {
            return (T)await LoadAsync(typeof(T), path);
        }

        public async Task<object> LoadAsync(Type type, string path)
        {
            return await DeserializeObjectAsync(path, path, type, null);
        }

        public async Task<bool> ReloadAsync(object asset, string? newPath = null)
        {
            if (!loadedAssetReferences.TryGetValue(asset, out Reference reference))
            {
                return false;
            }

            string path = newPath ?? reference.Path;

            await DeserializeObjectAsync(reference.Path, path, asset.GetType(), asset);

            if (path != reference.Path)
            {
                loadedAssetPaths.Remove(reference.Path);
            }

            return true;
        }

        public async Task SaveAsync(string path, object asset, Type? storageType = null)
        {
            await SerializeObjectAsync(path, asset, storageType);
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

        private Reference? FindDeserializedObject(string path, Type type)
        {
            if (loadedAssetPaths.TryGetValue(path, out Reference reference))
            {
                if (type.IsAssignableFrom(reference.Object?.GetType()))
                {
                    return reference;
                }
                else
                {
                    throw new ArgumentException($"The specified type {type} does not correspond to the type in the asset file.", nameof(type));
                }
            }

            return null;
        }

        private void SetAssetObject(Reference reference)
        {
            loadedAssetPaths[reference.Path] = reference;
            loadedAssetReferences[reference.Object] = reference;
        }
    }
}
