using System;
using System.Collections.ObjectModel;
using System.Linq;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

#nullable enable

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class PropertyGridView : UserControl
    {
        public PropertyGridView()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };
        }

        public PropertyGridViewModel ViewModel => (PropertyGridViewModel)DataContext;
    }

    [ContentProperty(Name = nameof(TemplateDefinitions))]
    public class DataTypeTemplateSelector : DataTemplateSelector
    {
        public TemplateDefintionCollection TemplateDefinitions { get; } = new TemplateDefintionCollection();

        protected override DataTemplate SelectTemplateCore(object item)
        {
            TemplateDefinition? templateDefinition = TemplateDefinitions.FirstOrDefault(t => t.DataType.IsInstanceOfType(item));

            return templateDefinition?.DataTemplate ?? base.SelectTemplateCore(item);
        }
    }

    public class TemplateDefintionCollection : Collection<TemplateDefinition>
    {
    }

    public class TemplateDefinition : DependencyObject
    {
        public static readonly DependencyProperty DataTypeProperty = DependencyProperty.Register(nameof(DataType), typeof(Type), typeof(TemplateDefinition), new PropertyMetadata(null));

        public static readonly DependencyProperty DataTemplateProperty = DependencyProperty.Register(nameof(DataTemplate), typeof(DataTemplate), typeof(TemplateDefinition), new PropertyMetadata(null));

        public Type DataType
        {
            get => (Type)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        public DataTemplate DataTemplate
        {
            get => (DataTemplate)GetValue(DataTemplateProperty);
            set => SetValue(DataTemplateProperty, value);
        }
    }
}
