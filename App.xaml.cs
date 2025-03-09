using DJAI.Services;
using DJAI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace DJAI
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class App : Application
    {
        private Window? m_window;

        // Add this property to hold the service provider
        public ServiceProvider? ServiceProvider { get; private set; }

        public App()
        {
            try
            {
                this.InitializeComponent();
                this.UnhandledException += App_UnhandledException;

                // Configure services
                ConfigureServices();

                Debug.WriteLine("App constructor completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in App constructor: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }

        // Add this method to configure services
        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register your services
            services.AddSingleton<ILLMService, OpenAIService>();
            services.AddSingleton<AnthropicService>();

            // Register view models
            services.AddTransient<MainViewModel>();

            // Build service provider
            ServiceProvider = services.BuildServiceProvider();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"UNHANDLED EXCEPTION: {e.Message}");
            Debug.WriteLine(e.Exception.ToString());
            e.Handled = true;
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                Debug.WriteLine("OnLaunched method started");

                m_window = new MainWindow();
                Debug.WriteLine("MainWindow created successfully");
                m_window.Activate();
                Debug.WriteLine("MainWindow activated successfully");

                Debug.WriteLine("OnLaunched method completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in OnLaunched: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}
