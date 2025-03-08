using DJAI.Contracts;
using DJAI.Helpers;
using DJAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace DJAI
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; }
        public static new App Current => (App)Application.Current;

        private Window? m_window;

        public App()
        {
            this.InitializeComponent();
            Services = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Services
            services.AddSingleton<ICacheService, RedisCacheService>(provider =>
                new RedisCacheService(configuration["Redis:ConnectionString"] ?? "localhost:6379"));
            services.AddSingleton<ApiRateLimiter>();
            services.AddSingleton<MessageLimitHandler>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<AIServiceFactory>();
            services.AddTransient<ExportService>();

            return services.BuildServiceProvider();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // IMPORTANT: Set unpackaged app path for file access
            Windows.Storage.ApplicationData.Current.LocalFolder.Path = Directory.GetCurrentDirectory();

            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}