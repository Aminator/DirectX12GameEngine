using System.Numerics;
using DirectX12GameEngine.Core;
using DirectX12GameEngine.Editor.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class Shell : UserControl
    {
        public Shell()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };

            SolutionExplorerTabView.TabItemsChanged += (s, e) => SolutionExplorerColumnDefinition.Width = GridLength.Auto;

            TitleBarShadow.Receivers.Add(ContentPanel);
            SolutionExplorerShadow.Receivers.Add(MainEditorPanel);
            TerminalShadow.Receivers.Add(ContentEditorPanel);

            TitleBarPanel.Translation += new Vector3(0.0f, 0.0f, 32.0f);
            SolutionExplorerPanel.Translation += new Vector3(0.0f, 0.0f, 32.0f);
            TerminalPanel.Translation += new Vector3(0.0f, 0.0f, 32.0f);

            CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;

            UpdateTitleBarLayout(titleBar);

            titleBar.LayoutMetricsChanged += (s, e) => UpdateTitleBarLayout(s);

            Window.Current.SetTitleBar(CustomDragRegion);

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequested;
        }

        private async void OnCloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            Deferral deferral = e.GetDeferral();

            e.Handled = await ViewModel.SolutionExplorerTabView.GetUnclosableTabsAsync().CountAsync() > 0
                || await ViewModel.SolutionExplorer.MainTabView.GetUnclosableTabsAsync().CountAsync() > 0;

            deferral.Complete();
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            if (SolutionExplorerTabView.TabItems.Count == 0)
            {
                SolutionExplorerColumnDefinition.Width = new GridLength(200);
            }
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);

            if (SolutionExplorerTabView.TabItems.Count == 0)
            {
                SolutionExplorerColumnDefinition.Width = GridLength.Auto;
            }
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar titleBar)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CustomDragRegion.MinWidth = titleBar.SystemOverlayRightInset;
                ShellTitleBarInset.MinWidth = titleBar.SystemOverlayLeftInset;
            }
            else
            {
                CustomDragRegion.MinWidth = titleBar.SystemOverlayLeftInset;
                ShellTitleBarInset.MinWidth = titleBar.SystemOverlayRightInset;
            }

            CustomDragRegion.Height = ShellTitleBarInset.Height = titleBar.Height;
        }
    }
}
