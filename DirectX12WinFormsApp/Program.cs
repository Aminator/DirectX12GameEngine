using System;
using System.IO;
using System.Windows.Forms;
using DirectX12Game;
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

            Load += MyForm_Load;
        }

        private async void MyForm_Load(object sender, EventArgs e)
        {
            MyGame game = new MyGame(new GameContextWinForms(this));

            game.Content.RootFolder = await StorageFolder.GetFolderFromPathAsync(Directory.GetCurrentDirectory());

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
