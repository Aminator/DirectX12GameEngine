using System;

namespace DirectX12GameEngine.Core.Assets
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetContentTypeAttribute : Attribute
    {
        public AssetContentTypeAttribute(Type contentType)
        {
            ContentType = contentType;
        }

        public Type ContentType { get; }
    }
}
