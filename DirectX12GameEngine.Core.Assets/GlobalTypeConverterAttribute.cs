using System;

namespace DirectX12GameEngine.Core.Assets
{
    [AttributeUsage(AttributeTargets.All)]
    public class GlobalTypeConverterAttribute : Attribute
    {
        public GlobalTypeConverterAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
