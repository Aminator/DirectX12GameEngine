using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.MSBuild;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public interface ISolutionLoader
    {
        event EventHandler<RootFolderLoadedEventArgs>? RootFolderLoaded;

        event EventHandler<StorageLibraryChangedEventArgs>? StorageLibraryChanged;

        event AnyEventHandler? BuildMessageRaised;

        MSBuildWorkspace Workspace { get; }

        StorageFolder? RootFolder { get; }

        StorageFolder? TemporarySolutionFolder { get; }

        public ObservableCollection<AccessListEntry> RecentSolutions { get; }

        Task OpenSolutionWithPickerAsync();

        Task OpenRecentSolutionAsync(string token);

        Task OpenSolutionAsync(IStorageFolder folder);

        Task ApplyChangesAsync();

        Task LoadSolutionAsync();

        Task<bool> RestoreNuGetPackagesAsync();

        Task<bool> RestoreNuGetPackagesAsync(string projectFilePath);

        Task<bool> BuildAsync(string projectFilePath);

        string? GetSolutionProjectFilePath(string projectFilePath);
    }
}
