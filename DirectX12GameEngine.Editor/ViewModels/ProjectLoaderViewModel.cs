using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Commanding;
using DirectX12GameEngine.Mvvm.Messaging;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.StartScreen;

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
            string token;

            if (!StorageApplicationPermissions.MostRecentlyUsedList.CheckAccess(folder))
            {
                token = StorageApplicationPermissions.MostRecentlyUsedList.Add(folder, folder.Path);
                AccessListEntry accessListEntry = new AccessListEntry { Token = token, Metadata = folder.Path };
                RecentProjects.Add(accessListEntry);
            }
            else
            {
                token = RecentProjects.First(e => e.Metadata == folder.Path).Token;
            }

            if (JumpList.IsSupported())
            {
                JumpList jumpList = await JumpList.LoadCurrentAsync();
                jumpList.SystemGroupKind = JumpListSystemGroupKind.Recent;

                JumpListItem? existingJumpListItem = jumpList.Items.FirstOrDefault(j => j.Arguments == token);

                if (existingJumpListItem is null || existingJumpListItem.RemovedByUser)
                {
                    JumpListItem jumpListItem = JumpListItem.CreateWithArguments(token, folder.Name);
                    jumpListItem.Description = folder.Path;
                    jumpListItem.GroupName = "Recent";

                    jumpList.Items.Add(jumpListItem);
                }

                await jumpList.SaveAsync();
            }

            if (IsProjectLoaded)
            {
                await CoreApplication.RequestRestartAsync(token);
            }
            else
            {
                IsProjectLoaded = true;

                StorageFolderViewModel item = new StorageFolderViewModel(folder);
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
