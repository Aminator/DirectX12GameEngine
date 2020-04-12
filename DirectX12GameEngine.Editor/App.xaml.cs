using System;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using DirectX12GameEngine.Editor.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;

#nullable enable

namespace DirectX12GameEngine.Editor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

            UwpViewModelLocator locator = new UwpViewModelLocator();

            ServiceCollection services = new ServiceCollection();
            locator.ConfigureServices(services);

            Services = services.BuildServiceProvider();
        }

        public IServiceProvider Services { get; }

        public XboxGameBarWidget? XboxGameBarWidget { get; set; }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs e)
        {
            base.OnBackgroundActivated(e);

            IBackgroundTaskInstance taskInstance = e.TaskInstance;

            if (taskInstance.Task.Name == SolutionLoaderViewModel.StorageLibraryChangeTrackerTaskName)
            {
                SolutionLoaderViewModel solutionLoader = Services.GetRequiredService<SolutionLoaderViewModel>();
                await solutionLoader.ApplyChangesAsync();
            }
        }

        protected override void OnActivated(IActivatedEventArgs e)
        {
            if (e.Kind == ActivationKind.Protocol)
            {
                if (e is IProtocolActivatedEventArgs protocolActivatedEventArgs)
                {
                    string scheme = protocolActivatedEventArgs.Uri.Scheme;

                    if (scheme == "ms-gamebarwidget")
                    {
                        if (e is XboxGameBarWidgetActivatedEventArgs widgetActivatedEventArgs)
                        {
                            Frame rootFrame = CreateRootFrame();

                            if (widgetActivatedEventArgs.IsLaunchActivation)
                            {
                                XboxGameBarWidget = new XboxGameBarWidget(
                                    widgetActivatedEventArgs,
                                    Window.Current.CoreWindow,
                                    rootFrame);
                            }

                            ActivateApp(rootFrame, "");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = CreateRootFrame();

            if (!e.PrelaunchActivated)
            {
                ActivateApp(rootFrame, e.Arguments);
            }
        }

        private Frame CreateRootFrame()
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        private void ActivateApp(Frame rootFrame, string arguments)
        {
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), new MainPageNavigationParameters(Services.GetRequiredService<MainViewModel>(), arguments));
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }

    public class UwpViewModelLocator : ViewModelLocator
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddSingleton<IWindowManager, AppWindowManager>();
        }
    }

    public class AppWindowManager : IWindowManager
    {
        private const double MinWindowWidth = 440;
        private const double MinWindowHeight = 48;

        public async Task<bool> TryCreateNewWindowAsync(TabViewViewModel tabView, Size size)
        {
            AppWindow appWindow = await AppWindow.TryCreateAsync();

            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            Frame frame = new Frame();
            frame.Navigate(typeof(AppWindowPage), new TabViewNavigationParameters(tabView, appWindow));

            ElementCompositionPreview.SetAppWindowContent(appWindow, frame);

            WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(MinWindowWidth, MinWindowHeight));
            appWindow.RequestSize(size);

            appWindow.RequestMoveAdjacentToCurrentView();

            bool success = await appWindow.TryShowAsync();

            return success;
        }
    }
}
