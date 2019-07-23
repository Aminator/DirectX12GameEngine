#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class PropertyGridViewModel : ViewModelBase
    {
        private string assetName = "No selection";

        public string AssetName
        {
            get => assetName;
            set => Set(ref assetName, value);
        }
    }
}
