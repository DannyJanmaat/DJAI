using DJAI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DJAI.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

        public SettingsPage()
        {
            this.InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial selection when page loads
            if (ViewModel != null && ProviderComboBox != null)
            {
                ProviderComboBox.SelectedIndex = ViewModel.SelectedProviderIndex;
            }
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && sender is ComboBox comboBox)
            {
                ViewModel.SelectedProviderIndex = comboBox.SelectedIndex;
            }

            ArgumentNullException.ThrowIfNull(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Update UI when navigating to this page
            if (ViewModel != null && ProviderComboBox != null)
            {
                ProviderComboBox.SelectedIndex = ViewModel.SelectedProviderIndex;
            }
        }
    }
}