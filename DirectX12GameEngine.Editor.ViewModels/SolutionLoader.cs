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
    public class SolutionLoader : ISolutionLoader
    {
        private readonly SemaphoreSlim solutionLoadLock = new SemaphoreSlim(1, 1);
        private readonly ISdkManager sdkManager;

        static SolutionLoader()
        {
            typeof(AppContext).GetMethod("SetData").Invoke(null, new[] { "PLATFORM_RESOURCE_ROOTS", Directory.GetCurrentDirectory() });
            Environment.SetEnvironmentVariable("APPDATA", ApplicationData.Current.TemporaryFolder.Path);

            PropertyInfo propertyToReplace = typeof(HttpClientHandler).GetProperty(nameof(HttpClientHandler.Credentials));
            MethodInfo methodToReplace = propertyToReplace.GetSetMethod();

            MethodInfo methodToInject = typeof(SolutionLoader).GetMethod(nameof(SetCredentials));

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

        public SolutionLoader(ISdkManager sdkManager)
        {
            this.sdkManager = sdkManager;
            sdkManager.SetSdkEnvironmentVariables(sdkManager.ActiveSdk);

            Workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                { "RestorePackagesPath", Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "NuGet", "packages") },
                { "RestoreSources", "https://api.nuget.org/v3/index.json" },
                { "NoWin32Manifest", "true" }
            });
        }

        public const string StorageLibraryChangeTrackerTaskName = "StorageLibraryChangeTrackerTask";

        public event EventHandler<RootFolderLoadedEventArgs>? RootFolderLoaded;

        public event EventHandler<StorageLibraryChangedEventArgs>? StorageLibraryChanged;

        public event AnyEventHandler? BuildMessageRaised;

        public MSBuildWorkspace Workspace { get; }

        public StorageFolder? RootFolder { get; private set; }

        public StorageFolder? TemporarySolutionFolder { get; private set; }

        public ObservableCollection<AccessListEntry> RecentSolutions { get; } = new ObservableCollection<AccessListEntry>();

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

        public async Task LoadSolutionAsync()
        {
            if (RootFolder is null || sdkManager.ActiveSdk is null) return;

            if (TemporarySolutionFolder is null)
            {
                var files = await RootFolder.GetFilesAsync();

                if (files.FirstOrDefault(s => s.FileType == ".sln" || s.FileType.Contains("proj")) is StorageFile originalSolutionFile)
                {
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
                        await Task.Run(() => Workspace.OpenSolutionAsync(solutionFile.Path));
                    }
                    else if (files.FirstOrDefault(s => s.FileType.Contains("proj")) is StorageFile projectFile)
                    {
                        await Task.Run(() => Workspace.OpenProjectAsync(projectFile.Path));
                    }
                }
            }
        }

        public async Task<bool> RestoreNuGetPackagesAsync()
        {
            if (RootFolder is null) return false;

            bool success = false;

            foreach (Project project in Workspace.CurrentSolution.Projects)
            {
                try
                {
                    if (await RestoreNuGetPackagesAsync(project.FilePath!))
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

            return success;
        }

        public async Task<bool> RestoreNuGetPackagesAsync(string projectFilePath)
        {
            var msbuildProject = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadProject(projectFilePath, Workspace.Properties, "Current");

            BuildLogger logger = new BuildLogger(LoggerVerbosity.Minimal);
            logger.AnyEventRaised += BuildMessageRaised;

            bool success = await Task.Run(() => msbuildProject.Build("Restore", new[] { logger }));

            return success;
        }

        public async Task<bool> BuildAsync(string projectFilePath)
        {
            bool success = false;

            if (await RestoreNuGetPackagesAsync())
            {
                Project project = Workspace.CurrentSolution.Projects.FirstOrDefault(p => p.FilePath == projectFilePath);

                Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.HostServices.RegisterHostObject(projectFilePath, "CoreCompile", "Csc", new CscHostObject(project));

                var msbuildProject = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadProject(projectFilePath, Workspace.Properties, "Current");

                BuildLogger logger = new BuildLogger(LoggerVerbosity.Minimal);
                logger.AnyEventRaised += BuildMessageRaised;

                success = await Task.Run(() => msbuildProject.Build("Build", new[] { logger }));
            }

            return success;
        }

        public string? GetSolutionProjectFilePath(string projectFilePath)
        {
            if (TemporarySolutionFolder is null) return null;

            string relativeProjectFilePath = StorageExtensions.GetRelativePath(RootFolder!.Path, projectFilePath);
            return Path.Combine(TemporarySolutionFolder.Path, relativeProjectFilePath);
        }
    }

    public class BuildLogger : ILogger
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

    public class StorageLibraryChangedEventArgs : EventArgs
    {
        public StorageLibraryChangedEventArgs(IReadOnlyList<StorageLibraryChange> changes)
        {
            Changes = changes;
        }

        public IReadOnlyList<StorageLibraryChange> Changes { get; }
    }
}
