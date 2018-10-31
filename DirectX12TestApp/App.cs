using System;
using DirectX12Game;
using DirectX12GameEngine;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

[module: System.Runtime.CompilerServices.NonNullTypes]
namespace DX12TestApp
{
    public sealed class App : IFrameworkViewSource, IFrameworkView
    {
        private MyGame? game;

        public IFrameworkView CreateView()
        {
            return this;
        }

        public void Initialize(CoreApplicationView applicationView)
        {
            applicationView.Activated += ApplicationView_Activated;
            CoreApplication.Suspending += CoreApplication_Suspending;
        }

        private void ApplicationView_Activated(CoreApplicationView sender, IActivatedEventArgs args)
        {
            sender.CoreWindow.Activate();
        }

        public void SetWindow(CoreWindow window)
        {
            ExtendViewIntoTitleBar(true);
        }

        public void Load(string entryPoint)
        {
            Uri uri = new Uri("https://www.google.com/");
        }

        public void Run()
        {
            try
            {
                game = new MyGame(new GameContextCoreWindow(isHolographic: false));
                game.Run();
            }
            catch (Exception)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        public void Uninitialize()
        {
        }

        private void CoreApplication_Suspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            //game.Dispose();
            deferral.Complete();
        }

        private static void ExtendViewIntoTitleBar(bool extendViewIntoTitleBar)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = extendViewIntoTitleBar;

            if (extendViewIntoTitleBar)
            {
                ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
        }
    }

    public class Program
    {
        private static void Main()
        {
            CoreApplication.Run(new App());
        }
    }
}
