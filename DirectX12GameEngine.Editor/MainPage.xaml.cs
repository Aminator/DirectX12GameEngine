using System;
using System.IO;
using System.Reflection;
using DirectX12GameEngine.Engine;
using DirectX12GameEngine.Games;
using Nito.AsyncEx;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DirectX12GameEngine.Editor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private EditorGame game;
        private bool isLoaded;
        private readonly AsyncLock isLoadedLock = new AsyncLock();
        private readonly Entity rootEntity = new Entity("Root");

        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            game = new EditorGame(new GameContextXaml(swapChainPanel));
            game.Run();
        }

        private async void OpenFolder()
        {
            using (await isLoadedLock.LockAsync())
            {
                if (isLoaded) await CoreApplication.RequestRestartAsync("");

                FolderPicker folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                game.Content.RootFolder = folder;

                FileOpenPicker assemblyPicker = new FileOpenPicker();
                assemblyPicker.FileTypeFilter.Add(".dll");
                StorageFile assemblyFile = await assemblyPicker.PickSingleFileAsync();
                StorageFile assemblyFileCopy = await assemblyFile.CopyAsync(ApplicationData.Current.TemporaryFolder, assemblyFile.Name, NameCollisionOption.ReplaceExisting);

                Assembly.LoadFrom(assemblyFileCopy.Path);

                Entity scene = await game.Content.LoadAsync<Entity>(@"Assets\Scenes\Scene1");
                rootEntity.Children.Add(scene);
                game.SceneSystem.SceneInstance.RootEntity = rootEntity;

                isLoaded = true;
            }
        }
    }
}
