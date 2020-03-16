using System;
using DirectX12GameEngine.Editor.ViewModels;
using Microsoft.Build.Framework;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class TerminalView : UserControl
    {
        public TerminalView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.BuildMessageRaised += OnBuildMessageRaised;

            if (ViewModel.CurrentText != null)
            {
                Terminal.TextDocument.GetText(TextGetOptions.AdjustCrlf | TextGetOptions.UseCrlf, out string text);

                if (text != ViewModel.CurrentText)
                {
                    Terminal.TextDocument.SetText(TextSetOptions.None, ViewModel.CurrentText);
                }
            }
        }

        public TerminalViewModel ViewModel => (TerminalViewModel)DataContext;

        private void OnBuildMessageRaised(object sender, BuildEventArgs e)
        {
            //await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //{
            //    if (!string.IsNullOrEmpty(e.Message))
            //    {
            //        Terminal.Document.Selection.TypeText(e.Message);
            //        Terminal.Document.Selection.TypeText(Environment.NewLine);
            //    }
            //});
        }
    }
}
