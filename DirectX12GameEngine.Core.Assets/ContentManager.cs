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
            AddTypeConverters(typeof(Vector3Converter), typeof(Vector4Converter), typeof(QuaternionConverter), typeof(Matrix4x4Converter));

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddAssembly(assembly);
            }

            AppDomain.CurrentDomain.AssemblyLoad += (s, e) => AddAssembly(e.LoadedAssembly);
        }

        private static void AddAssembly(Assembly assembly)
        {
            foreach (ContractNamespaceAttribute attribute in assembly.GetCustomAttributes<ContractNamespaceAttribute>())
            {
                if (!XmlnsDefinitions.TryGetValue(attribute.ContractNamespace, out var namespaces))
                {
                    namespaces = new List<(string, Assembly)>();
                    XmlnsDefinitions.Add(attribute.ContractNamespace, namespaces);
                }

                namespaces.Add((attribute.ClrNamespace, assembly));
            }
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

        public static Dictionary<string, List<(string ClrNamespace, Assembly Assembly)>> XmlnsDefinitions { get; } = new Dictionary<string, List<(string, Assembly)>>();

        public IServiceProvider Services { get; }

        public string FileExtension { get; set; } = ".xaml";

        public StorageFolder RootFolder { get; set; }

        public string RootPath => RootFolder.Path;

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

            return properties.Where(p => !p.IsDefined(typeof(IgnoreDataMemberAttribute)) && !p.IsSpecialName && !(p.GetIndexParameters().Length > 0));
        }

        public static void GetNamespaceAndTypeName(string xmlName, XElement element, out string namespaceName, out string typeName)
        {
            string[] namespaceAndType = xmlName.Split(new[] { ':' }, 2);

            if (namespaceAndType.Length == 2)
            {
                string namespacePrefix = namespaceAndType[0];
                namespaceName = element.GetNamespaceOfPrefix(namespacePrefix).NamespaceName;

                typeName = namespaceAndType[1];
            }
            else
            {
                namespaceName = element.GetDefaultNamespace().NamespaceName;
                typeName = xmlName;
            }
        }

        public static Type GetTypeFromXmlName(string xmlNamespace, string typeName)
        {
            const string clrNamespaceString = "clr-namespace:";
            const string assemblyString = "assembly=";
            const string extensionString = "Extension";

            int indexOfSemicolon = xmlNamespace.IndexOf(';');

            if (xmlNamespace.StartsWith(clrNamespaceString))
            {
                string namespaceName = indexOfSemicolon >= 0
                    ? xmlNamespace.Substring(clrNamespaceString.Length, indexOfSemicolon - clrNamespaceString.Length)
                    : xmlNamespace.Substring(clrNamespaceString.Length);

                Assembly assembly;

                if (indexOfSemicolon >= 0)
                {
                    string assemblyName = xmlNamespace.Substring(indexOfSemicolon + assemblyString.Length + 1);
                    assembly = Assembly.Load(assemblyName);
                }
                else
                {
                    assembly = Assembly.GetExecutingAssembly();
                }

                Type? type = assembly.GetType(namespaceName + Type.Delimiter + typeName, false);

                return type ?? assembly.GetType(namespaceName + Type.Delimiter + typeName + extensionString, true);
            }
            else
            {
                var namespaces = XmlnsDefinitions[xmlNamespace];

                foreach ((string clrNamespace, Assembly assembly) in namespaces)
                {
                    Type? type = assembly.GetType(clrNamespace + Type.Delimiter + typeName, false);
                    type ??= assembly.GetType(clrNamespace + Type.Delimiter + typeName + extensionString, false);

                    if (type != null)
                    {
                        return type;
                    }
                }

                throw new InvalidOperationException();
            }
        }
    }
}
