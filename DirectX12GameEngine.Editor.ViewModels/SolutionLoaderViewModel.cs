using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.Mvvm.Commands;
using Microsoft.Toolkit.Mvvm.ObjectModel;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SolutionLoaderViewModel : ObservableObject
    {
        private readonly ISolutionLoader solutionLoader;

        private bool isSolutionLoading;

        public SolutionLoaderViewModel(ISolutionLoader solutionLoader)
        {
            this.solutionLoader = solutionLoader;

            foreach (AccessListEntry accessListEntry in StorageApplicationPermissions.MostRecentlyUsedList.Entries)
            {
                solutionLoader.RecentSolutions.Add(accessListEntry);
            }

            OpenSolutionWithPickerCommand = new RelayCommand(() => _ = solutionLoader.OpenSolutionWithPickerAsync());
            OpenRecentSolutionCommand = new RelayCommand<string>(token => _ = solutionLoader.OpenRecentSolutionAsync(token));
            ReloadSolutionCommand = new RelayCommand(() => _ = solutionLoader.LoadSolutionAsync());
            RestoreNuGetPackagesCommand = new RelayCommand(() => _ = solutionLoader.RestoreNuGetPackagesAsync());
        }

        public bool IsSolutionLoading
        {
            get => isSolutionLoading;
            private set => Set(ref isSolutionLoading, value);
        }

        public ObservableCollection<AccessListEntry> RecentSolutions => solutionLoader.RecentSolutions;

        public RelayCommand OpenSolutionWithPickerCommand { get; }

        public RelayCommand<string> OpenRecentSolutionCommand { get; }

        public RelayCommand ReloadSolutionCommand { get; }

        public RelayCommand RestoreNuGetPackagesCommand { get; }
    }
}
