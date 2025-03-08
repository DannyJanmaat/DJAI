using DJAI.Contracts;
using DJAI.Helpers;
using DJAI.Services;
using DJAI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.Versioning;

namespace DJAI
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            SetWindowTitle();

            // Zorg voor een handle voor de ExportService
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Haal services op via DI container
            IServiceProvider serviceProvider = App.Current.Services;

            // Pattern matching gebruiken voor code verbetering
            if (serviceProvider.GetService(typeof(AIServiceFactory)) is not AIServiceFactory aiServiceFactory ||
                serviceProvider.GetService(typeof(ApiRateLimiter)) is not ApiRateLimiter rateLimiter ||
                serviceProvider.GetService(typeof(MessageLimitHandler)) is not MessageLimitHandler messageLimitHandler ||
                serviceProvider.GetService(typeof(ICacheService)) is not ICacheService cacheService ||
                serviceProvider.GetService(typeof(SettingsService)) is not SettingsService settingsService)
            {
                throw new InvalidOperationException("Kon niet alle vereiste services initialiseren.");
            }

            // ExportService apart behandelen omdat deze niet altijd geregistreerd is
            ExportService exportService = serviceProvider.GetService(typeof(ExportService)) as ExportService ??
                               new ExportService(windowHandle);

            // Maak hoofdviewmodel aan
            ViewModel = new MainViewModel(
                aiServiceFactory,
                rateLimiter,
                messageLimitHandler,
                cacheService,
                settingsService,
                exportService);

            // ExtendsContentIntoTitleBar moet na InitializeComponent
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null); // Volledig aangepaste titelbalk
        }

        private void SetWindowTitle()
        {
            Title = "DJAI - AI Chat";
        }

        private void DeleteConversation_Tapped(object sender, TappedRoutedEventArgs _)
        {
            ViewModel.DeleteSelectedConversation();
        }
    }
}
