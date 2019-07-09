using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using Nito.AsyncEx;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

#nullable enable

namespace DirectX12GameEngine.Editor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private EditorGame? game;
        private bool isLoaded;
        private readonly AsyncLock isLoadedLock = new AsyncLock();

        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;
        }

        public EntityViewModel RootEntity { get; } = new EntityViewModel(new Entity("Root"));

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (AccessListEntry accessListEntry in StorageApplicationPermissions.FutureAccessList.Entries)
            {
                string token = accessListEntry.Token;
                StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);

                MenuFlyoutItem item = new MenuFlyoutItem { Text = folder.Path };
                item.Click += Item_Click;
                openRecentMenuFlyoutItem.Items.Add(item);
            }

            game = new EditorGame(new GameContextXaml(swapChainPanel));
            game.Run();

            game.SceneSystem.SceneInstance.RootEntity = RootEntity;
        }

        private async Task OpenFolderAsync(StorageFolder folder)
        {
            if (game is null) return;

            using (await isLoadedLock.LockAsync())
            {
                if (isLoaded) await CoreApplication.RequestRestartAsync("");

                game.Content.RootFolder = folder;

                StorageFolder assemblyFolder = await folder.GetFolderAsync(@"bin\Debug\netstandard2.0");
                StorageFile assemblyFile = await assemblyFolder.GetFileAsync(folder.Name + ".dll");
                StorageFile assemblyFileCopy = await assemblyFile.CopyAsync(ApplicationData.Current.TemporaryFolder, assemblyFile.Name, NameCollisionOption.ReplaceExisting);

                Assembly.LoadFrom(assemblyFileCopy.Path);

                Entity scene = await game.Content.LoadAsync<Entity>(@"Assets\Scenes\Scene1");

                EntityViewModel sceneViewModel = new EntityViewModel(scene);
                RootEntity.Children.Add(sceneViewModel);

                isLoaded = true;
            }
        }

        private void TreeView_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
        {
            componentsListView.Items.Clear();

            if (args.InvokedItem is EntityViewModel entity)
            {
                foreach (EntityComponentViewModel component in entity.Components)
                {
                    componentsListView.Items.Add(new TextBlock { Text = component.TypeName });
                }
            }
        }

        private async void Item_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            string path = item.Text;

            AccessListEntry accessListEntry = StorageApplicationPermissions.FutureAccessList.Entries.First(e => e.Metadata == path);
            StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(accessListEntry.Token);

            await OpenFolderAsync(folder);
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (game is null) return;

            if (RootEntity.Children.Count > 0)
            {
                Entity scene = RootEntity.Children[0];

                await game.Content.SaveAsync(@"Assets\Scenes\Scene1", scene);
            }
        }

        private async void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            StorageApplicationPermissions.FutureAccessList.Add(folder, folder.Path);
            await OpenFolderAsync(folder);
        }
    }
}
