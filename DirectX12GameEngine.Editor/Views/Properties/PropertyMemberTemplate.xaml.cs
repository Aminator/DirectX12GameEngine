using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views.Properties
{
    public sealed partial class PropertyMemberTemplate : UserControl
    {
        public PropertyMemberTemplate()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty MemberColorProperty = DependencyProperty.Register(nameof(MemberColor), typeof(Brush), typeof(PropertyMemberTemplate), new PropertyMetadata(default));

        public static readonly DependencyProperty MemberNameProperty = DependencyProperty.Register(nameof(MemberName), typeof(string), typeof(PropertyMemberTemplate), new PropertyMetadata(default));

        public static readonly DependencyProperty MemberValueProperty = DependencyProperty.Register(nameof(MemberValue), typeof(string), typeof(PropertyMemberTemplate), new PropertyMetadata(default));

        public Brush MemberColor
        {
            get => (Brush)GetValue(MemberColorProperty);
            set => SetValue(MemberColorProperty, value);
        }

        public string MemberName
        {
            get => (string)GetValue(MemberNameProperty);
            set => SetValue(MemberNameProperty, value);
        }

        public string MemberValue
        {
            get => (string)GetValue(MemberValueProperty);
            set => SetValue(MemberValueProperty, value);
        }
    }
}
