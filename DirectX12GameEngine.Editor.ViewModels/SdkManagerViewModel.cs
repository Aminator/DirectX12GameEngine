using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.Commands;
using Microsoft.Toolkit.Mvvm.ObjectModel;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SdkManagerViewModel : ObservableObject
    {
        private readonly ISdkManager sdkManager;

        public SdkManagerViewModel(ISdkManager sdkManager)
        {
            this.sdkManager = sdkManager;
            sdkManager.ActiveSdkChanged += (s, e) => OnPropertyChanged(nameof(ActiveSdk));

            DownloadSdkCommand = new RelayCommand<string>(version => _ = sdkManager.DownloadSdkAsync(version));
            OpenSdkWithPickerCommand = new RelayCommand(() => _ = sdkManager.OpenSdkWithPickerAsync());
            RemoveSdkCommand = new RelayCommand<SdkViewModel>(sdk => _ = sdkManager.RemoveSdkAsync(sdk));
        }

        public ObservableCollection<SdkViewModel> RecentSdks => sdkManager.RecentSdks;

        public RelayCommand<string> DownloadSdkCommand { get; }

        public RelayCommand OpenSdkWithPickerCommand { get; }

        public RelayCommand<SdkViewModel> RemoveSdkCommand { get; }

        public SdkViewModel? ActiveSdk
        {
            get => sdkManager.ActiveSdk;
            set => Set(sdkManager.ActiveSdk, value, () => sdkManager.ActiveSdk = value);
        }
    }
}
