using System;

namespace DirectX12GameEngine.Serialization
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
