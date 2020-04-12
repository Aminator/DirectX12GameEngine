using System;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

#nullable enable

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class PropertyManagerView : UserControl
    {
        public PropertyManagerView()
        {
            InitializeComponent();
        }

        public PropertyManagerViewModel ViewModel => (PropertyManagerViewModel)DataContext;
    }

    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (DateTimeOffset)(DateTime)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((DateTimeOffset)value).UtcDateTime;
        }
    }
}
