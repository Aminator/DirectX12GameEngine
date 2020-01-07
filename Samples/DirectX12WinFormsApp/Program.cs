using System;
using System.IO;
using System.Windows.Forms;
using DirectX12Game;
using DirectX12GameEngine.Serialization;
using DirectX12GameEngine.Games;
using Windows.Storage;

namespace DirectX12WinFormsApp
{
    public class MyForm : Form
    {
        public MyForm()
        {
            Width = 1200;
            Height = 800;

            Load += OnMyFormLoad;
        }

        private async void OnMyFormLoad(object? sender, EventArgs e)
        {
            StorageFolder rootFolder = await StorageFolder.GetFolderFromPathAsync(Directory.GetCurrentDirectory());

            MyGame game = new MyGame(new WinFormsGameContext(this) { FileProvider = new FileSystemProvider(rootFolder) });
            game.Run();
        }
    }

    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyForm());
        }
    }
}
