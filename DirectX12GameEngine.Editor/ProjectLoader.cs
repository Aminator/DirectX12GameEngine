using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

#nullable enable

namespace DirectX12GameEngine.Editor
{
    public class ProjectLoader : ViewModelBase
    {
        private bool isLoading;
        private bool isProjectLoaded;

        public ProjectLoader()
        {
            foreach (AccessListEntry accessListEntry in StorageApplicationPermissions.MostRecentlyUsedList.Entries)
            {
                RecentProjects.Add(accessListEntry);
            }
        }

        public SolutionExplorer SolutionExplorer { get; } = new SolutionExplorer();

        public bool IsLoading
        {
            get => isLoading;
            set => Set(ref isLoading, value);
        }

        public bool IsProjectLoaded
        {
            get => isProjectLoaded;
            private set => Set(ref isProjectLoaded, value);
        }

        public ObservableCollection<AccessListEntry> RecentProjects { get; } = new ObservableCollection<AccessListEntry>();

        public async Task OpenAssetAsync()
        {
            throw new NotImplementedException();
        }

        public async Task OpenRecentProjectAsync(string token)
        {
            StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);

            await OpenProjectAsync(folder);
        }

        public async Task OpenProjectWithPickerAsync()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            await OpenProjectAsync(folder);
        }

        public async Task OpenProjectAsync(StorageFolder folder)
        {
            if (!StorageApplicationPermissions.MostRecentlyUsedList.CheckAccess(folder))
            {
                string token = StorageApplicationPermissions.MostRecentlyUsedList.Add(folder, folder.Path);
                AccessListEntry accessListEntry = new AccessListEntry { Token = token, Metadata = folder.Path };
                RecentProjects.Add(accessListEntry);
            }

            if (IsProjectLoaded)
            {
                string token = RecentProjects.First(e => e.Metadata == folder.Path).Token;
                await CoreApplication.RequestRestartAsync(token);
            }
            else
            {
                IsLoading = true;
                IsProjectLoaded = true;

                StorageItemViewModel item = new StorageItemViewModel(folder);
                await SolutionExplorer.SetRootFolderAsync(item);

                await LoadAssemblyAsync(folder);
                IsLoading = false;
            }
        }

        public async Task SaveProjectAsync()
        {
            throw new NotImplementedException();
        }

        private async Task LoadAssemblyAsync(StorageFolder folder)
        {
            try
            {
                StorageFile assemblyFile = await folder.GetFileAsync(Path.Combine(@"bin\Debug\netstandard2.0", folder.Name + ".dll"));
                StorageFile assemblyFileCopy = await assemblyFile.CopyAsync(ApplicationData.Current.TemporaryFolder, assemblyFile.Name, NameCollisionOption.ReplaceExisting);

                Assembly.LoadFrom(assemblyFileCopy.Path);
            }
            finally
            {
            }
        }
    }
}
