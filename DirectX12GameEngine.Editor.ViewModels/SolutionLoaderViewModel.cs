using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Nito.AsyncEx;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.StartScreen;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionLoaderViewModel : ViewModelBase
    {
        public const string StorageLibraryChangeTrackerTaskName = "StorageLibraryChangeTrackerTask";

        private readonly SemaphoreSlim solutionLoadLock = new SemaphoreSlim(1, 1);

        private bool isSolutionLoading;

        static SolutionLoaderViewModel()
        {
            typeof(AppContext).GetMethod("SetData").Invoke(null, new[] { "PLATFORM_RESOURCE_ROOTS", Directory.GetCurrentDirectory() });
            Environment.SetEnvironmentVariable("APPDATA", ApplicationData.Current.TemporaryFolder.Path);

            PropertyInfo propertyToReplace = typeof(HttpClientHandler).GetProperty(nameof(HttpClientHandler.Credentials));
            MethodInfo methodToReplace = propertyToReplace.GetSetMethod();

            MethodInfo methodToInject = typeof(SolutionLoaderViewModel).GetMethod(nameof(SetCredentials));

            ReplaceMethod(methodToReplace, methodToInject);
        }

        public static unsafe void ReplaceMethod(MethodInfo methodToReplace, MethodInfo methodToInject)
        {
            RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);
            RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);

            int* handleToReplace = (int*)methodToReplace.MethodHandle.Value.ToPointer();
            int* handleToInject = (int*)methodToInject.MethodHandle.Value.ToPointer();

            handleToReplace += 2;
            handleToInject += 2;

            *handleToReplace = *handleToInject;
        }

        public void SetCredentials(ICredentials credentials)
        {
        }

        public SolutionLoaderViewModel(SdkManagerViewModel sdkManager)
        {
            sdkManager.SetSdkEnvironmentVariables(sdkManager.ActiveSdk);

            Workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                { "RestorePackagesPath", Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "NuGet", "packages") },
                { "RestoreSources", "https://api.nuget.org/v3/index.json" }
            });

            foreach (AccessListEntry accessListEntry in StorageApplicationPermissions.MostRecentlyUsedList.Entries)
            {
                RecentSolutions.Add(accessListEntry);
            }

            OpenSolutionWithPickerCommand = new RelayCommand(() => _ = OpenSolutionWithPickerAsync());
            OpenRecentSolutionCommand = new RelayCommand<string>(token => _ = OpenRecentSolutionAsync(token));
            ReloadSolutionCommand = new RelayCommand(() => _ = LoadSolutionAsync());
            RestoreNuGetPackagesCommand = new RelayCommand(() => _ = RestoreNuGetPackagesAsync());
        }

        public event EventHandler<RootFolderLoadedEventArgs>? RootFolderLoaded;

        public event EventHandler<StorageLibraryChangedEventArgs>? StorageLibraryChanged;

        public event AnyEventHandler? BuildMessageRaised;

        public MSBuildWorkspace Workspace { get; }

        public StorageFolder? RootFolder { get; private set; }

        public StorageFolder? TemporarySolutionFolder { get; private set; }

        public ObservableCollection<AccessListEntry> RecentSolutions { get; } = new ObservableCollection<AccessListEntry>();

        public bool IsSolutionLoading
        {
            get => isSolutionLoading;
            private set => Set(ref isSolutionLoading, value);
        }

        public RelayCommand OpenSolutionWithPickerCommand { get; }

        public RelayCommand<string> OpenRecentSolutionCommand { get; }

        public RelayCommand ReloadSolutionCommand { get; }

        public RelayCommand RestoreNuGetPackagesCommand { get; }

        public Task<StorageFolder> GetTemporarySolutionsFolderAsync()
        {
            return ApplicationData.Current.TemporaryFolder.CreateFolderAsync("Solutions", CreationCollisionOption.OpenIfExists).AsTask();
        }

        public async Task OpenSolutionWithPickerAsync()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder? folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                await OpenSolutionAsync(folder);
            }
        }

        public async Task OpenRecentSolutionAsync(string token)
        {
            StorageFolder folder = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(token);

            await OpenSolutionAsync(folder);
        }

        public async Task OpenSolutionAsync(IStorageFolder folder)
        {
            AccessListEntry accessListEntry = RecentSolutions.FirstOrDefault(e => e.Metadata == folder.Path);

            if (string.IsNullOrEmpty(accessListEntry.Token))
            {
                string token = StorageApplicationPermissions.MostRecentlyUsedList.Add(folder, folder.Path);
                accessListEntry = new AccessListEntry { Token = token, Metadata = folder.Path };
                RecentSolutions.Add(accessListEntry);
            }

            if (JumpList.IsSupported())
            {
                JumpList jumpList = await JumpList.LoadCurrentAsync();
                jumpList.SystemGroupKind = JumpListSystemGroupKind.Recent;

                JumpListItem? existingJumpListItem = jumpList.Items.FirstOrDefault(j => j.Arguments == accessListEntry.Token);

                if (existingJumpListItem is null || existingJumpListItem.RemovedByUser)
                {
                    JumpListItem jumpListItem = JumpListItem.CreateWithArguments(accessListEntry.Token, folder.Name);
                    jumpListItem.Description = folder.Path;
                    jumpListItem.GroupName = "Recent";

                    jumpList.Items.Add(jumpListItem);
                }

                await jumpList.SaveAsync();
            }

            if (RootFolder != null)
            {
                await CoreApplication.RequestRestartAsync(accessListEntry.Token);
            }
            else
            {
                RootFolder = (StorageFolder)folder;

                await RegisterBackgroundTaskAsync();

                RootFolderLoaded?.Invoke(this, new RootFolderLoadedEventArgs(RootFolder));

                await LoadSolutionAsync();
            }
        }

        private async Task<bool> RegisterBackgroundTaskAsync()
        {
            StorageLibraryChangeTracker? tracker = RootFolder?.TryGetChangeTracker();

            if (tracker != null)
            {
                tracker.Enable();

                StorageLibraryChangeTrackerTrigger trigger = new StorageLibraryChangeTrackerTrigger(tracker);

                await BackgroundExecutionManager.RequestAccessAsync();

                BackgroundTaskBuilder builder = new BackgroundTaskBuilder
                {
                    Name = StorageLibraryChangeTrackerTaskName
                };

                builder.SetTrigger(trigger);
                builder.Register();

                return true;
            }

            return false;
        }

        public async Task ApplyChangesAsync()
        {
            StorageLibraryChangeTracker? tracker = RootFolder?.TryGetChangeTracker();

            if (tracker != null)
            {
                tracker.Enable();
                StorageLibraryChangeReader changeReader = tracker.GetChangeReader();
                var changes = await changeReader.ReadBatchAsync();
                await changeReader.AcceptChangesAsync();

                if (changes.Count > 0)
                {
                    Task applyChangesTask = ApplyChangesToTemporaryFolderAsync(changes);

                    StorageLibraryChanged?.Invoke(this, new StorageLibraryChangedEventArgs(changes));

                    await applyChangesTask;

                    await LoadSolutionAsync();
                }
            }
        }

        private async Task ApplyChangesToTemporaryFolderAsync(IReadOnlyList<StorageLibraryChange> changes)
        {
            if (RootFolder is null || TemporarySolutionFolder is null) return;

            foreach (StorageLibraryChange change in changes)
            {
                if (Path.GetFileName(change.Path).StartsWith(".")
                    || Path.GetFileName(change.PreviousPath).StartsWith(".")) continue;

                switch (change.ChangeType)
                {
                    case StorageLibraryChangeType.Created:
                    case StorageLibraryChangeType.ContentsChanged:
                    case StorageLibraryChangeType.MovedIntoLibrary:
                    case StorageLibraryChangeType.ContentsReplaced:
                        {
                            string relativePath = StorageExtensions.GetRelativePath(RootFolder.Path, change.Path);
                            string relativeDirectory = Path.GetDirectoryName(relativePath);

                            IStorageItem? destinationItem = relativePath.Contains(Path.DirectorySeparatorChar) ? await TemporarySolutionFolder.TryGetItemAsync(relativeDirectory) : TemporarySolutionFolder;
                            IStorageItem? item = await change.GetStorageItemAsync();

                            if (destinationItem is IStorageFolder destinationFolder)
                            {
                                if (item is IStorageFile file)
                                {
                                    await file.CopyAsync(destinationFolder, file.Name, NameCollisionOption.ReplaceExisting);
                                }
                                else if (item is IStorageFolder folder)
                                {
                                    await folder.CopyAsync(destinationFolder, NameCollisionOption.ReplaceExisting);
                                }
                            }
                        }
                        break;
                    case StorageLibraryChangeType.Deleted:
                    case StorageLibraryChangeType.MovedOutOfLibrary:
                        {
                            string relativePath = StorageExtensions.GetRelativePath(RootFolder.Path, change.Path);
                            IStorageItem? item = await TemporarySolutionFolder.TryGetItemAsync(relativePath);

                            if (item != null)
                            {
                                await item.DeleteAsync();
                            }
                        }
                        break;
                    case StorageLibraryChangeType.MovedOrRenamed:
                        {
                            string relativePath = StorageExtensions.GetRelativePath(RootFolder.Path, change.Path);
                            string relativeDirectory = Path.GetDirectoryName(relativePath);
                            string previousRelativePath = StorageExtensions.GetRelativePath(RootFolder.Path, change.PreviousPath);

                            IStorageItem? destinationItem = relativePath.Contains(Path.DirectorySeparatorChar) ? await TemporarySolutionFolder.TryGetItemAsync(relativeDirectory) : TemporarySolutionFolder;
                            IStorageItem? item = await TemporarySolutionFolder.GetItemAsync(previousRelativePath);

                            if (item != null)
                            {
                                if (Path.GetDirectoryName(change.Path).Equals(Path.GetDirectoryName(change.PreviousPath), StringComparison.OrdinalIgnoreCase))
                                {
                                    await item.RenameAsync(Path.GetFileName(change.Path));
                                }
                                else
                                {
                                    if (destinationItem is IStorageFolder destinationFolder)
                                    {
                                        if (item is IStorageFile file)
                                        {
                                            await file.MoveAsync(destinationFolder, file.Name, NameCollisionOption.ReplaceExisting);
                                        }
                                        else if (item is IStorageFolder folder)
                                        {
                                            await folder.MoveAsync(destinationFolder, NameCollisionOption.ReplaceExisting);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }

            }
        }

        private async Task LoadSolutionAsync()
        {
            if (RootFolder is null) return;

            if (TemporarySolutionFolder is null)
            {
                var files = await RootFolder.GetFilesAsync();

                if (files.FirstOrDefault(s => s.FileType == ".sln" || s.FileType.Contains("proj")) is StorageFile originalSolutionFile)
                {
                    IsSolutionLoading = true;

                    StorageFolder solutionsFolder = await GetTemporarySolutionsFolderAsync();
                    TemporarySolutionFolder = await RootFolder.CopyAsync(solutionsFolder, NameCollisionOption.ReplaceExisting, "bin", "obj");
                }
            }

            using (await solutionLoadLock.LockAsync())
            {
                Workspace.CloseSolution();

                if (TemporarySolutionFolder != null)
                {
                    var files = await TemporarySolutionFolder.GetFilesAsync();

                    if (files.FirstOrDefault(s => s.FileType == ".sln") is StorageFile solutionFile)
                    {
                        IsSolutionLoading = true;

                        await Task.Run(() => Workspace.OpenSolutionAsync(solutionFile.Path));
                    }
                    else if (files.FirstOrDefault(s => s.FileType.Contains("proj")) is StorageFile projectFile)
                    {
                        IsSolutionLoading = true;

                        await Task.Run(() => Workspace.OpenProjectAsync(projectFile.Path));
                    }
                }
            }

            IsSolutionLoading = false;
        }

        private async Task RestoreNuGetPackagesAsync()
        {
            if (RootFolder is null) return;

            IsSolutionLoading = true;

            bool success = false;

            foreach (Project project in Workspace.CurrentSolution.Projects)
            {
                try
                {
                    if (await RestoreNuGetPackagesAsync(project.FilePath!, Workspace.Properties))
                    {
                        success = true;
                    }
                }
                catch
                {
                }
            }

            if (success)
            {
                await LoadSolutionAsync();
            }

            IsSolutionLoading = false;
        }

        private async Task<bool> RestoreNuGetPackagesAsync(string projectFilePath, IDictionary<string, string> properties)
        {
            var msbuildProject = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadProject(projectFilePath, properties, "Current");

            BuildLogger logger = new BuildLogger(LoggerVerbosity.Normal);
            logger.AnyEventRaised += BuildMessageRaised;

            return await Task.Run(() => msbuildProject.Build("Restore", new[] { logger }));
        }

        private class BuildLogger : ILogger
        {
            public BuildLogger(LoggerVerbosity verbosity)
            {
                Verbosity = verbosity;
            }

            public event AnyEventHandler? AnyEventRaised;

            public LoggerVerbosity Verbosity { get; set; }

            public string? Parameters { get; set; }

            public void Initialize(IEventSource eventSource)
            {
                eventSource.AnyEventRaised += AnyEventRaised;
            }

            public void Shutdown()
            {
            }
        }
    }

    public class StorageLibraryChangedEventArgs : EventArgs
    {
        public StorageLibraryChangedEventArgs(IReadOnlyList<StorageLibraryChange> changes)
        {
            Changes = changes;
        }

        public IReadOnlyList<StorageLibraryChange> Changes { get; }
    }
}
