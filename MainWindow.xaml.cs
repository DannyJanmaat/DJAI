using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace DJAI
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                Debug.WriteLine("MainWindow constructor started");
                this.InitializeComponent();
                this.Title = "DJAI Chat";

                // Navigate to the main page if using a frame
                ContentFrame?.Navigate(typeof(Views.ChatPage));

                Debug.WriteLine("MainWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in MainWindow constructor: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}
