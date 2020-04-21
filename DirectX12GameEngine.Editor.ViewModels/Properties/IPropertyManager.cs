using System;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public interface IPropertyManager
    {
        event EventHandler<PropertyViewEventArgs>? PropertyViewRequested;

        void ShowProperties(object value);
    }
}
