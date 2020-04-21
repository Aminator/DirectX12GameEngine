using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public interface ISdkManager
    {
        public event EventHandler? ActiveSdkChanged;

        SdkViewModel? ActiveSdk { get; set; }

        ObservableCollection<SdkViewModel> RecentSdks { get; }

        Task DownloadSdkAsync(string version);

        Task OpenSdkWithPickerAsync();

        Task RemoveSdkAsync(SdkViewModel sdk);

        void SetSdkEnvironmentVariables(SdkViewModel? sdk);
    }
}
