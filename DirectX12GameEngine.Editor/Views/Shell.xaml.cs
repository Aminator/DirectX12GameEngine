using System;
using System.Collections.Generic;
using System.Numerics;
using DirectX12GameEngine.Editor.Factories;
using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Editor.Messaging;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Engine;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

            SolutionExplorerTabView.Items.VectorChanged += (s, e) => SolutionExplorerColumnDefinition.Width = GridLength.Auto;

            SolutionExplorerShadow.Receivers.Add(AssetEditorTabView);
            //SolutionExplorerShadow.Receivers.Add(BackgroundPanel);
            //AssetEditorShadow.Receivers.Add(BackgroundPanel);

            AssetEditorTabView.Translation += new Vector3(0.0f, 0.0f, 32.0f);
            SolutionExplorerTabView.Translation += new Vector3(0.0f, 0.0f, 64.0f);

            Window.Current.SetTitleBar(TitleBar);

            EngineAssetViewFactory factory = new EngineAssetViewFactory();
            factory.Add(typeof(Entity), new SceneViewFactory());

            AssetViewFactory.Default.Add(".xaml", factory);

            RegisterMessages();
        }

        public MainViewModel ViewModel => (MainViewModel)DataContext;

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            if (SolutionExplorerTabView.Items.Count == 0)
            {
                SolutionExplorerColumnDefinition.Width = new GridLength(200);
            }
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);

            if (SolutionExplorerTabView.Items.Count == 0)
            {
                SolutionExplorerColumnDefinition.Width = GridLength.Auto;
            }
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<LaunchStorageItemMessage>(this, async m =>
            {
                if (m.Item is StorageFileViewModel file)
                {
                    object? uiElement = await AssetViewFactory.Default.CreateAsync(file);

                    if (uiElement != null)
                    {
                        TabViewItem tabViewItem = new TabViewItem
                        {
                            Content = uiElement,
                            Header = m.Item.Name
                        };

                        AssetEditorTabView.Items.Add(tabViewItem);
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

                    SolutionExplorerTabView.Items.Add(tabViewItem);
                    visibleViews.Add(m.ViewName, tabViewItem);
                }
                else if (m.ViewName == "PropertyGrid")
                {
                    TabViewItem tabViewItem = new TabViewItem
                    {
                        Header = "Properties",
                        Content = new PropertyGridView { DataContext = ViewModel.PropertyGrid }
                    };

                    tabViewItem.Closing += (s, e) =>
                    {
                        e.Cancel = true;
                        ViewModel.EditorViews.IsPropertyGridOpen = false;
                    };

                    SolutionExplorerTabView.Items.Add(tabViewItem);
                    visibleViews.Add(m.ViewName, tabViewItem);
                }
            });

            Messenger.Default.Register<CloseViewMessage>(this, m =>
            {
                string viewName = m.ViewName;

                if (visibleViews.TryGetValue(viewName, out TabViewItem item))
                {
                    (item.Parent as TabView)?.Items.Remove(item);
                    visibleViews.Remove(viewName);
                }
            });
        }
    }
}
