using DirectX12GameEngine.Editor.ViewModels;
using WinUI = Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class EditorMenuBar : WinUI.MenuBar
    {
        public EditorMenuBar()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;
    }
}
