using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SdkManager : ISdkManager
    {
        public const string ActiveSdkVersionKey = "ActiveSdkVersion";

        private SdkViewModel? activeSdk;

        public SdkManager()
        {
            string sdksPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Sdks");

            if (Directory.Exists(sdksPath))
            {
                foreach (string path in Directory.GetDirectories(sdksPath))
                {
                    SdkViewModel sdk = new SdkViewModel(new DirectoryInfo(path).Name, path)
                    {
                        DownloadProgess = 1,
                        InstallProgess = 1
                    };

                    RecentSdks.Add(sdk);
                }
            }

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(ActiveSdkVersionKey, out object storedActiveSdkVersion))
            {
                SdkViewModel? sdk = RecentSdks.FirstOrDefault(s => s.Version == (string)storedActiveSdkVersion);

                if (sdk != null)
                {
                    ActiveSdk = sdk;
                }
                else
                {
                    ApplicationData.Current.LocalSettings.Values.Remove(ActiveSdkVersionKey);
                }
            }

            if (ActiveSdk is null)
            {
                SdkViewModel? newestSdk = RecentSdks.OrderByDescending(s => s.Version).FirstOrDefault();

                if (newestSdk != null)
                {
                    ActiveSdk = newestSdk;
                }
            }
        }

        public event EventHandler? ActiveSdkChanged;

        public SdkViewModel? ActiveSdk
        {
            get => activeSdk;
            set
            {
                activeSdk = value;
                ApplyActiveSdk(activeSdk);
            }
        }

        public ObservableCollection<SdkViewModel> RecentSdks { get; } = new ObservableCollection<SdkViewModel>();

        public Task<StorageFolder> GetSdksFolderAsync()
        {
            return ApplicationData.Current.LocalFolder.CreateFolderAsync("Sdks", CreationCollisionOption.OpenIfExists).AsTask();
        }

        public async Task DownloadSdkAsync(string version)
        {
            StorageFolder sdksFolder = await GetSdksFolderAsync();
            SdkViewModel sdk = new SdkViewModel(version, Path.Combine(sdksFolder.Path, version));

            if (!RecentSdks.Contains(sdk))
            {
                RecentSdks.Add(sdk);

                string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
                string sdkZipFileName = $"dotnet-sdk-{version}-win-{architecture}.zip";

                if (!(await sdksFolder.TryGetItemAsync(sdkZipFileName) is StorageFile sdkZipFile))
                {
                    Uri azureFeed = new Uri("https://dotnetcli.azureedge.net/dotnet/Sdk/");
                    Uri downloadLink = new Uri(azureFeed, $"{version}/{sdkZipFileName}");

                    using WebClient client = new WebClient();

                    sdk.DownloadProgess = 0;
                    client.DownloadProgressChanged += (s, e) => sdk.DownloadProgess = (double)e.ProgressPercentage / 100;

                    await client.DownloadFileTaskAsync(downloadLink, Path.Combine(sdksFolder.Path, sdkZipFileName));

                    sdkZipFile = await sdksFolder.GetFileAsync(sdkZipFileName);
                }

                sdk.DownloadProgess = 1;

                using (Stream sdkZipFileStream = await sdkZipFile.OpenStreamForReadAsync())
                using (ZipArchive zipArchive = new ZipArchive(sdkZipFileStream, ZipArchiveMode.Read))
                {
                    var archiveEntries = zipArchive.Entries.Where(e => e.FullName.StartsWith("sdk"));
                    int archiveEntryCount = archiveEntries.Count();
                    int archiveEntryCounter = 0;
                    sdk.InstallProgess = 0;

                    foreach (ZipArchiveEntry archiveEntry in archiveEntries)
                    {
                        string relativeArchiveEntryPath = StorageExtensions.GetRelativePath("sdk", archiveEntry.FullName);

                        using Stream fileStream = await sdksFolder.OpenStreamForWriteAsync(relativeArchiveEntryPath, CreationCollisionOption.ReplaceExisting);
                        using Stream archiveEntryStream = archiveEntry.Open();

                        if (Path.GetFileName(relativeArchiveEntryPath) == "Microsoft.Build.Tasks.CodeAnalysis.dll")
                        {
                            using FileStream codeAnalysisLibrary = new FileStream("Microsoft.Build.Tasks.CodeAnalysis.dll", FileMode.Open, FileAccess.Read);
                            await codeAnalysisLibrary.CopyToAsync(fileStream);
                        }
                        else
                        {
                            await archiveEntryStream.CopyToAsync(fileStream);
                        }

                        archiveEntryCounter++;
                        sdk.InstallProgess = (double)archiveEntryCounter / archiveEntryCount;
                    }
                }

                await sdkZipFile.DeleteAsync(StorageDeleteOption.PermanentDelete);

                ActiveSdk = sdk;
                ActiveSdkChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task OpenSdkWithPickerAsync()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder? folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                StorageFolder sdksFolder = await GetSdksFolderAsync();
                SdkViewModel sdk = new SdkViewModel(folder.Name, Path.Combine(sdksFolder.Path, folder.Name))
                {
                    DownloadProgess = 1
                };

                if (!RecentSdks.Contains(sdk))
                {
                    RecentSdks.Add(sdk);

                    StorageFolder sdkFolder = await folder.CopyAsync(sdksFolder, NameCollisionOption.ReplaceExisting,
                        new Progress<double>(p => sdk.InstallProgess = p));

                    ActiveSdk = sdk;
                    ActiveSdkChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public async Task RemoveSdkAsync(SdkViewModel sdk)
        {
            RecentSdks.Remove(sdk);

            StorageFolder sdksFolder = await GetSdksFolderAsync();

            if (await sdksFolder.TryGetItemAsync(sdk.Version.ToString()) is StorageFolder sdkFolder)
            {
                await sdkFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        public void SetSdkEnvironmentVariables(SdkViewModel? sdk)
        {
            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", sdk is null ? null : Path.Combine(sdk.Path, "MSBuild.dll"));
            Environment.SetEnvironmentVariable("MSBuildExtensionsPath", sdk?.Path);
            Environment.SetEnvironmentVariable("MSBuildSdksPath", sdk is null ? null : Path.Combine(sdk.Path, "Sdks"));
        }

        private void ApplyActiveSdk(SdkViewModel? sdk)
        {
            if (sdk != null)
            {
                ApplicationData.Current.LocalSettings.Values[ActiveSdkVersionKey] = sdk.Version;
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values.Remove(ActiveSdkVersionKey);
            }

            SetSdkEnvironmentVariables(sdk);
        }
    }
}
