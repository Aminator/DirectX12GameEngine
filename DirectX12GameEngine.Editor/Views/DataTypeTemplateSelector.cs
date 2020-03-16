using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    [ContentProperty(Name = nameof(TemplateDefinitions))]
    public class DataTypeTemplateSelector : DataTemplateSelector
    {
        public TemplateDefinitionCollection TemplateDefinitions { get; } = new TemplateDefinitionCollection();

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return TemplateDefinitions.FirstOrDefault(t => t.DataType == item.GetType()).DataTemplate ?? throw new InvalidOperationException("The data template was null.");
        }
    }

    public class TemplateDefinitionCollection : Collection<TemplateDefinition>
    {
    }

    public class TemplateDefinition
    {
        public Type? DataType { get; set; }

        public DataTemplate? DataTemplate { get; set; }
    }
}
