// Update file: Views/ChatPage.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using DJAI.ViewModels;

namespace DJAI.Views
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public sealed partial class ChatPage : Page
    {
        // Make this a public property for binding
        public MainViewModel? ViewModel { get; private set; }

        public ChatPage()
        {
            // Get the service provider from the App class
            var app = Microsoft.UI.Xaml.Application.Current as App;
            if (app?.ServiceProvider != null)
            {
                // Get the view model from the service provider
                ViewModel = app.ServiceProvider.GetService<MainViewModel>();
            }
            else
            {
                // Create a default view model if service provider is not available
                ViewModel = new MainViewModel();
                System.Diagnostics.Debug.WriteLine("Service provider not available, using default ViewModel");
            }

            this.InitializeComponent();
            this.DataContext = ViewModel;
        }
    }
}
