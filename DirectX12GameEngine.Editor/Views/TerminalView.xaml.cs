using System;
using DirectX12GameEngine.Editor.ViewModels;
using Microsoft.Build.Framework;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class TerminalView : UserControl
    {
        private readonly DispatcherQueue dispatcherQueue;

        public TerminalView()
        {
            InitializeComponent();

            dispatcherQueue = Window.Current.CoreWindow.DispatcherQueue;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
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

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.BuildMessageRaised -= OnBuildMessageRaised;
        }

        public TerminalViewModel ViewModel => (TerminalViewModel)DataContext;

        private void OnBuildMessageRaised(object sender, BuildEventArgs e)
        {
            dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (!string.IsNullOrEmpty(e.Message))
                {
                    Terminal.Document.Selection.TypeText(e.Message + '\r');
                    Terminal.Document.Selection.ScrollIntoView(PointOptions.NoHorizontalScroll);
                }
            });
        }
    }
}
