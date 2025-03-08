using DJAI.Contracts;
using DJAI.Helpers;
using DJAI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

#nullable enable

namespace DJAI.Views
{
    public sealed partial class ChatPage : Page
    {
        public ChatViewModel ViewModel { get; }

        public ChatPage()
        {
            this.InitializeComponent();

            // Normaal gesproken zou je deze ViewModel injecteren via DI
            // Maar voor de eenvoud maken we het hier direct
            var services = App.Current.Services;

            // Use pattern matching for cleaner code
            if (services.GetService(typeof(IAIService)) is IAIService aiService &&
                services.GetService(typeof(DJAI.Helpers.ApiRateLimiter)) is DJAI.Helpers.ApiRateLimiter rateLimiter &&
                services.GetService(typeof(DJAI.Helpers.MessageLimitHandler)) is DJAI.Helpers.MessageLimitHandler messageLimitHandler &&
                services.GetService(typeof(ICacheService)) is ICacheService cacheService)
            {
                ViewModel = new ChatViewModel(aiService, rateLimiter, messageLimitHandler, cacheService);
            }
            else
            {
                throw new InvalidOperationException("Required services not available.");
            }
        }

        private void UserInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !e.KeyStatus.IsExtendedKey &&
                sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text) &&
                !ViewModel.IsGenerating)
            {
                ViewModel.SendMessageCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}