using System;
using System.Collections.Generic;
using System.Numerics;
using DirectX12GameEngine.Editor.Factories;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Mvvm.Messaging;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Engine;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class Shell : UserControl
    {
        private readonly Dictionary<string, TabViewItem> visibleViews = new Dictionary<string, TabViewItem>();

        public Shell()
        {
            InitializeComponent();

            DataContextChanged += (s, e) =>
            {
                Bindings.Update();
            };

            SolutionExplorerTabView.TabItemsChanged += (s, e) => SolutionExplorerColumnDefinition.Width = GridLength.Auto;

            TitleBarShadow.Receivers.Add(ContentPanel);
            SolutionExplorerShadow.Receivers.Add(AssetEditorPanel);

            TitleBarPanel.Translation += new Vector3(0.0f, 0.0f, 32.0f);
            SolutionExplorerPanel.Translation += new Vector3(0.0f, 0.0f, 32.0f);

            CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;

            UpdateTitleBarLayout(titleBar);

            titleBar.LayoutMetricsChanged += (s, e) => UpdateTitleBarLayout(s);

            Window.Current.SetTitleBar(CustomDragRegion);

            EngineAssetViewFactory engineAssetViewFactory = new EngineAssetViewFactory();
            engineAssetViewFactory.Add(typeof(Entity), new SceneViewFactory());

            CodeEditorViewFactory codeEditorViewFactory = new CodeEditorViewFactory();

            AssetViewFactory.Default.Add(".xaml", engineAssetViewFactory);
            AssetViewFactory.Default.Add(".cs", codeEditorViewFactory);

            RegisterMessages();
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

        private void RegisterMessages()
        {
            Messenger.Default.Register<LaunchStorageItemMessage>(this, async m =>
            {
                if (m.Item is StorageFileViewModel file)
                {
                    object? element = await AssetViewFactory.Default.CreateAsync(file);

                    if (element != null)
                    {
                        AddEditorView(element, m.Item.Name);
                    }
                    else
                    {
                        await Launcher.LaunchFileAsync(file.Model);
                    }
                }
                else if (m.Item is StorageFolderViewModel folder)
                {
                    await Launcher.LaunchFolderAsync(folder.Model);
                }
            });

            Messenger.Default.Register<ViewCodeMessage>(this, async m =>
            {
                object? element = await new CodeEditorViewFactory().CreateAsync(m.File);

                if (element != null)
                {
                    AddEditorView(element, m.File.Name);
                }
                else
                {
                    await Launcher.LaunchFileAsync(m.File.Model);
                }
            });

            Messenger.Default.Register<OpenViewMessage>(this, m =>
            {
                if (m.ViewName == "SolutionExplorer")
                {
                    TabViewItem tabViewItem = new TabViewItem
                    {
                        Header = "Solution Explorer",
                        Content = new SolutionExplorerView { DataContext = ViewModel.SolutionExplorer }
                    };

                    tabViewItem.CloseRequested += (s, e) =>
                    {
                        ViewModel.EditorViews.IsSolutionExplorerOpen = false;
                    };

                    SolutionExplorerTabView.TabItems.Add(tabViewItem);
                    visibleViews.Add(m.ViewName, tabViewItem);
                }
                else if (m.ViewName == "PropertyGrid")
                {
                    TabViewItem tabViewItem = new TabViewItem
                    {
                        Header = "Properties",
                        Content = new PropertyGridView { DataContext = ViewModel.PropertyGrid }
                    };

                    tabViewItem.CloseRequested += (s, e) =>
                    {
                        ViewModel.EditorViews.IsPropertyGridOpen = false;
                    };

                    SolutionExplorerTabView.TabItems.Add(tabViewItem);
                    visibleViews.Add(m.ViewName, tabViewItem);
                }
            });

            Messenger.Default.Register<CloseViewMessage>(this, m =>
            {
                string viewName = m.ViewName;

                if (visibleViews.TryGetValue(viewName, out TabViewItem item))
                {
                    (item.Parent as TabViewListView)?.Items.Remove(item);
                    visibleViews.Remove(viewName);
                }
            });
        }

        private void AddEditorView(object element, string name)
        {
            TabViewItem tabViewItem = new TabViewItem
            {
                Content = element,
                Header = name
            };

            AssetEditorTabView.TabItems.Add(tabViewItem);
        }
    }
}
