using System;
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
        public Shell()
        {
            InitializeComponent();

            Window.Current.SetTitleBar(titleBar);

            EngineAssetViewFactory factory = new EngineAssetViewFactory();
            factory.Add(typeof(Entity), new SceneViewFactory());

            AssetViewFactory.Default.Add(".xaml", factory);

            Messenger.Default.Register<LaunchStorageItemMessage>(this, async m =>
            {
                UIElement? uiElement = await AssetViewFactory.Default.CreateAsync(m.Item);

                if (uiElement != null)
                {
                    TabViewItem tab = new TabViewItem
                    {
                        Header = m.Item.Name,
                        Content = uiElement
                    };

                    ExtendedTabView tabView = new ExtendedTabView();
                    tabView.Items.Add(tab);

                    dockPanel.Children.Add(tabView);
                    DockPanel.SetDock(tabView, Dock.Right);
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
        }

        public EditorViewModel ViewModel { get; } = new EditorViewModel();
    }
}
