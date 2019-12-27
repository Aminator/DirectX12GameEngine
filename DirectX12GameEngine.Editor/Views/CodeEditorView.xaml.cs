using System;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class CodeEditorView : UserControl
    {
        public CodeEditorView()
        {
            InitializeComponent();

            DataContextChanged += async (s, e) =>
            {
                //Bindings.Update();

                if (ViewModel.File != null)
                {
                    var stream = await ViewModel.File.Model.OpenReadAsync();
                    CodeEditor.Document.LoadFromStream(TextSetOptions.None, stream);
                }
            };
        }

        public CodeEditorViewModel ViewModel => (CodeEditorViewModel)DataContext;
    }
}
