using System;
using System.Linq;
using DirectX12GameEngine.Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class EditorTabView : TabView
    {
        private const string TabKey = "Tab";
        private const string TabViewKey = "TabView";

        public EditorTabView()
        {
            InitializeComponent();

            TabItemsChanged += OnTabItemsChanged;
            //TabStripDragOver += OnTabStripDragOver;
            TabDragStarting += OnTabDragStarting;
            TabDroppedOutside += OnTabDroppedOutside;
            TabCloseRequested += OnTabCloseRequested;
        }

        public TabViewViewModel ViewModel => (TabViewViewModel)DataContext;

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            ViewModel.Focus();
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            e.AcceptedOperation = DataPackageOperation.Move;
        }

        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.DataView != null)
            {
                if (e.DataView.Properties.TryGetValue(TabKey, out object tab)
                    && e.DataView.Properties.TryGetValue(TabViewKey, out object tabView) && tabView is TabViewViewModel originalTabView)
                {
                    originalTabView.Tabs.Remove(tab);
                    ViewModel.Tabs.Add(tab);
                }
                else if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();

                    foreach (IStorageFile file in items.OfType<IStorageFile>())
                    {
                        StorageApplicationPermissions.FutureAccessList.Add(file, file.Path);

                        CodeEditorViewModel codeEditor = new CodeEditorViewModel(file);
                        ViewModel.Tabs.Add(codeEditor);
                    }
                }
            }
        }

        private void OnTabItemsChanged(TabView sender, IVectorChangedEventArgs e)
        {
            if (e.CollectionChange == CollectionChange.ItemInserted)
            {
                SelectedIndex = (int)e.Index;
            }
        }

        //private void OnTabStripDragOver(object sender, DragEventArgs e)
        //{
        //    e.AcceptedOperation = DataPackageOperation.Move;
        //}

        private void OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs e)
        {
            e.Data.Properties.Add(TabKey, e.Item);
            e.Data.Properties.Add(TabViewKey, ViewModel);
        }

        private void OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs e)
        {
            ViewModel.Tabs.Remove(e.Item);

            double scaling = XamlRoot.RasterizationScale;

            ViewModel.OnTabDroppedOutside(new TabDroppedOutsideEventArgs(e.Item, new Size(ActualWidth * scaling, ActualHeight * scaling)));
        }

        private async void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs e)
        {
            await ViewModel.TryCloseTabAsync(e.Item);
        }
    }
}
