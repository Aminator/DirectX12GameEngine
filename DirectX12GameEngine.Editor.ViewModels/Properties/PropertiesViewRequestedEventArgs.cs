using System;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public class PropertiesViewRequestedEventArgs : EventArgs
    {
        public PropertiesViewRequestedEventArgs(object obj)
        {
            Object = obj;
        }

        public object Object { get; }
    }
}
