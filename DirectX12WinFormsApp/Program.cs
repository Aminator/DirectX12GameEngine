using DirectX12Game;
using DirectX12GameEngine;
using System;
using System.Windows.Forms;

namespace DirectX12WinFormsApp
{
    class MyForm : Form
    {
        public MyForm()
        {
            Width = 1200;
            Height = 800;

            Activated += MyForm_Activated;
        }

        private void MyForm_Activated(object sender, EventArgs e)
        {
            MyGame game = new MyGame(new GameContextWinForms(this));
            game.Run();
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyForm());
        }
    }
}
