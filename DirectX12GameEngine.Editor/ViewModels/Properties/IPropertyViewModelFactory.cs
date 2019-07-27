using System.Reflection;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public interface IPropertyViewModelFactory
    {
        public PropertyViewModel Create(object model, PropertyInfo propertyInfo);
    }
}
