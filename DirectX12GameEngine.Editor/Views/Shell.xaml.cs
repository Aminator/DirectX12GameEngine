using System;
using System.Collections.Generic;
using System.Numerics;
using DirectX12GameEngine.Editor.Factories;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Engine;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

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

            solutionExplorerTabView.Items.VectorChanged += (s, e) => solutionExplorerColumnDefinition.Width = GridLength.Auto;

            SolutionExplorerShadow.Receivers.Add(assetEditorTabView);

            assetEditorTabView.Translation += new Vector3(0.0f, 0.0f, 32.0f);
            solutionExplorerTabView.Translation += new Vector3(0.0f, 0.0f, 64.0f);

            Window.Current.SetTitleBar(titleBar);

            EngineAssetViewFactory factory = new EngineAssetViewFactory();
            factory.Add(typeof(Entity), new SceneViewFactory());

            AssetViewFactory.Default.Add(".xaml", factory);

            RegisterMessages();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            if (solutionExplorerTabView.Items.Count == 0)
            {
                solutionExplorerColumnDefinition.Width = new GridLength(200);
            }
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);

            if (solutionExplorerTabView.Items.Count == 0)
            {
                solutionExplorerColumnDefinition.Width = GridLength.Auto;
            }
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<LaunchStorageItemMessage>(this, async m =>
            {
                object? uiElement = await AssetViewFactory.Default.CreateAsync(m.Item);

                if (uiElement != null)
                {
                    TabViewItem tabViewItem = new TabViewItem
                    {
                        Content = uiElement,
                        Header = m.Item.Name
                    };

                    assetEditorTabView.Items.Add(tabViewItem);
                }
                else
                {
                    if (m.Item.Model is IStorageFile file)
                    {
                        await Launcher.LaunchFileAsync(file);
                    }
                    else if (m.Item.Model is IStorageFolder folder)
                    {
                        await Launcher.LaunchFolderAsync(folder);
                    }
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

                    tabViewItem.Closing += (s, e) =>
                    {
                        e.Cancel = true;
                        ViewModel.EditorViews.IsSolutionExplorerOpen = false;
                    };

                    solutionExplorerTabView.Items.Add(tabViewItem);
                    visibleViews.Add(m.ViewName, tabViewItem);
                }
                else if (m.ViewName == "PropertyGrid")
                {
                    TabViewItem tabViewItem = new TabViewItem
                    {
                        Header = "Property Grid",
                        Content = new PropertyGridView { DataContext = ViewModel.PropertyGrid }
                    };

                    tabViewItem.Closing += (s, e) =>
                    {
                        e.Cancel = true;
                        ViewModel.EditorViews.IsPropertyGridOpen = false;
                    };

                    solutionExplorerTabView.Items.Add(tabViewItem);
                    visibleViews.Add(m.ViewName, tabViewItem);
                }
            });

            Messenger.Default.Register<CloseViewMessage>(this, m =>
            {
                if (visibleViews.TryGetValue(m.ViewName, out TabViewItem item))
                {
                    (item.Parent as TabView)?.Items.Remove(item);
                    visibleViews.Remove(m.ViewName);
                }
            });
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;
    }
}
