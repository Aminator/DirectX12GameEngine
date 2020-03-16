using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.StartScreen;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionLoaderViewModel : ViewModelBase
    {
        private bool isRootFolderLoaded;
        private bool isSolutionLoaded;
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

            if (IntPtr.Size == 4)
            {
                handleToReplace += 2;
                handleToInject += 2;
            }
            else
            {
                handleToReplace += 1;
                handleToInject += 1;
            }

            *handleToReplace = *handleToInject;
        }

        public void SetCredentials(ICredentials credentials)
        {
        }

        public SolutionLoaderViewModel()
        {
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

        public event AnyEventHandler? BuildMessageRaised;

        public MSBuildWorkspace Workspace { get; }

        public IStorageFolder? RootFolder { get; private set; }

        public ObservableCollection<AccessListEntry> RecentSolutions { get; } = new ObservableCollection<AccessListEntry>();

        public bool IsRootFolderLoaded
        {
            get => isRootFolderLoaded;
            private set => Set(ref isRootFolderLoaded, value);
        }

        public bool IsSolutionLoaded
        {
            get => isSolutionLoaded;
            private set => Set(ref isSolutionLoaded, value);
        }

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

            if (IsRootFolderLoaded)
            {
                await CoreApplication.RequestRestartAsync(accessListEntry.Token);
            }
            else
            {
                RootFolder = folder;

                IsRootFolderLoaded = true;
                RootFolderLoaded?.Invoke(this, new RootFolderLoadedEventArgs(RootFolder));

                await LoadSolutionAsync();
            }
        }

        public async Task LoadSolutionAsync()
        {
            if (RootFolder is null) return;

            IsSolutionLoading = true;

            StorageFile? originalSolutionFile = (await RootFolder.GetFilesAsync()).FirstOrDefault(s => s.FileType == ".sln");

            if (originalSolutionFile != null)
            {
                StorageFolder solutionsFolder = await GetTemporarySolutionsFolderAsync();

                if (!(await solutionsFolder.TryGetItemAsync(RootFolder.Name) is StorageFolder solutionFolder))
                {
                    solutionFolder = await RootFolder.CopyAsync(solutionsFolder, NameCollisionOption.ReplaceExisting, ".vs", ".git", "bin", "obj");
                }

                StorageFile? solutionFile = (await solutionFolder.GetFilesAsync()).FirstOrDefault(s => s.FileType == ".sln");

                if (solutionFile != null)
                {
                    await Task.Run(async () => await Workspace.OpenSolutionAsync(solutionFile.Path));
                }
            }

            IsSolutionLoading = false;
        }

        private async Task RestoreNuGetPackagesAsync()
        {
            if (RootFolder is null) return;

            IsSolutionLoading = true;

            bool success = false;

            try
            {
                foreach (Project project in Workspace.CurrentSolution.Projects)
                {
                    if (await RestoreNuGetPackagesAsync(project.FilePath!, Workspace.Properties))
                    {
                        success = true;
                    }
                }
            }
            catch
            {
            }

            if (success)
            {
                await Task.Run(async () => await Workspace.OpenSolutionAsync(Workspace.CurrentSolution.FilePath));
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
}
