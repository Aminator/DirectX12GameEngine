using System;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public class PropertyManager : IPropertyManager
    {
        public event EventHandler<PropertyViewEventArgs>? PropertyViewRequested;

        public void ShowProperties(object value)
        {
            PropertyViewRequested?.Invoke(this, new PropertyViewEventArgs(value));
        }
    }

    public class PropertyViewEventArgs
    {
        public PropertyViewEventArgs(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
