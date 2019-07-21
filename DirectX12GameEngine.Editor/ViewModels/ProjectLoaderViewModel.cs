using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.Commanding;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class ProjectLoaderViewModel : ViewModelBase
    {
        private bool isProjectLoaded;

        public ProjectLoaderViewModel()
        {
            foreach (AccessListEntry accessListEntry in StorageApplicationPermissions.MostRecentlyUsedList.Entries)
            {
                RecentProjects.Add(accessListEntry);
            }

            OpenProjectWithPickerCommand = new RelayCommand(async () => await OpenProjectWithPickerAsync());
            OpenRecentProjectCommand = new RelayCommand<string>(async token => await OpenRecentProjectAsync(token));
        }

        public bool IsProjectLoaded
        {
            get => isProjectLoaded;
            private set => Set(ref isProjectLoaded, value);
        }

        public ObservableCollection<AccessListEntry> RecentProjects { get; } = new ObservableCollection<AccessListEntry>();

        public RelayCommand OpenProjectWithPickerCommand { get; }

        public RelayCommand<string> OpenRecentProjectCommand { get; }

        public async Task OpenProjectWithPickerAsync()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder? folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                await OpenProjectAsync(folder);
            }
        }

        public async Task OpenRecentProjectAsync(string token)
        {
            StorageFolder folder = await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(token);

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
                IsProjectLoaded = true;

                StorageItemViewModel item = new StorageItemViewModel(folder);
                Messenger.Default.Send<ProjectLoadedMessage>(new ProjectLoadedMessage(item));

                await LoadAssemblyAsync(folder);
            }
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
