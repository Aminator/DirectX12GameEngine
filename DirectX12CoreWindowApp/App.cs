using System;
using DirectX12Game;
using DirectX12GameEngine.Games;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Preview.Holographic;
using Windows.Graphics.Holographic;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

#nullable enable

namespace DirectX12CoreWindowApp
{
    public sealed class App : IFrameworkViewSource, IFrameworkView
    {
        private MyGame? game;
        private GameContext? gameContext;

        public IFrameworkView CreateView() => this;

        public void Initialize(CoreApplicationView applicationView)
        {
            applicationView.Activated += OnApplicationViewActivated;
            CoreApplication.Suspending += OnCoreApplicationSuspending;
        }

        private void OnApplicationViewActivated(CoreApplicationView sender, IActivatedEventArgs e)
        {
            if (HolographicApplicationPreview.IsCurrentViewPresentedOnHolographicDisplay())
            {
                HolographicSpace holographicSpace = HolographicSpace.CreateForCoreWindow(sender.CoreWindow);

                gameContext = new HolographicGameContext(holographicSpace);
            }
            else
            {
                gameContext = new CoreWindowGameContext();
            }

            sender.CoreWindow.Activate();
        }

        public void SetWindow(CoreWindow window)
        {
            ExtendViewIntoTitleBar(true);
        }

        public void Load(string entryPoint)
        {
        }

        public void Run()
        {
            if (gameContext is null) throw new InvalidOperationException();

            game = new MyGame(gameContext);
            game.Run();
        }

        public void Uninitialize()
        {
        }

        private void OnCoreApplicationSuspending(object sender, SuspendingEventArgs e)
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
