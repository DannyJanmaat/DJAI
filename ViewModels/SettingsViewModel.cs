using DJAI.Commands;
using DJAI.Models;
using DJAI.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Input;

#nullable enable

namespace DJAI.ViewModels
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private readonly Dictionary<AIProvider, string> _apiKeys = [];
        private readonly Dictionary<AIProvider, AIProviderSettings> _providerSettings = [];
        private AIProvider _selectedProvider = AIProvider.Anthropic;
        private bool _isSaving;
        private readonly string[] _providerNames = ["Anthropic Claude", "OpenAI GPT", "GitHub Copilot"];

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                if (_isSaving != value)
                {
                    _isSaving = value;
                    OnPropertyChanged();
                }
            }
        }

        public AIProvider SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (_selectedProvider != value)
                {
                    _selectedProvider = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedProviderIndex));
                    OnPropertyChanged(nameof(SelectedProviderName));
                    OnPropertyChanged(nameof(CurrentApiKey));
                    OnPropertyChanged(nameof(CurrentSettings));
                }
            }
        }

        public int SelectedProviderIndex
        {
            get => (int)_selectedProvider;
            set
            {
                if ((int)_selectedProvider != value && value >= 0 && value < Enum.GetValues<AIProvider>().Length)
                {
                    _selectedProvider = (AIProvider)value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedProvider));
                    OnPropertyChanged(nameof(SelectedProviderName));
                    OnPropertyChanged(nameof(CurrentApiKey));
                    OnPropertyChanged(nameof(CurrentSettings));
                }
            }
        }

        public string SelectedProviderName
        {
            get => _providerNames[(int)_selectedProvider];
            set
            {
                int index = Array.IndexOf(_providerNames, value);
                if (index >= 0 && (int)_selectedProvider != index)
                {
                    _selectedProvider = (AIProvider)index;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedProvider));
                    OnPropertyChanged(nameof(SelectedProviderIndex));
                    OnPropertyChanged(nameof(CurrentApiKey));
                    OnPropertyChanged(nameof(CurrentSettings));
                }
            }
        }

        public string CurrentApiKey
        {
            get
            {
                if (_apiKeys.TryGetValue(SelectedProvider, out var apiKey))
                {
                    return apiKey;
                }
                return string.Empty;
            }
            set
            {
                _apiKeys[SelectedProvider] = value;
                OnPropertyChanged();
            }
        }

        public AIProviderSettings CurrentSettings
        {
            get
            {
                if (_providerSettings.TryGetValue(SelectedProvider, out var settings))
                {
                    return settings;
                }
                return new AIProviderSettings { Provider = SelectedProvider };
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            SaveCommand = new RelayCommand(async _ => await SaveSettingsAsync());
            ResetCommand = new RelayCommand(_ => LoadSettings());

            LoadSettings();
        }

        private void LoadSettings()
        {
            foreach (AIProvider provider in Enum.GetValues<AIProvider>())
            {
                // Load API keys
                _apiKeys[provider] = _settingsService.GetApiKey(provider);

                // Load provider-specific settings
                var settingsKey = $"{provider}_settings";

                // Get settings as string and deserialize manually since AIProviderSettings is a class (reference type)
                string settingsJson = _settingsService.GetSetting(settingsKey, "");
                AIProviderSettings settings;

                if (string.IsNullOrEmpty(settingsJson))
                {
                    settings = new AIProviderSettings { Provider = provider };
                }
                else
                {
                    try
                    {
                        settings = System.Text.Json.JsonSerializer.Deserialize<AIProviderSettings>(settingsJson)
                                  ?? new AIProviderSettings { Provider = provider };
                    }
                    catch
                    {
                        settings = new AIProviderSettings { Provider = provider };
                    }
                }

                _providerSettings[provider] = settings;
            }

            OnPropertyChanged(nameof(SelectedProvider));
            OnPropertyChanged(nameof(SelectedProviderIndex));
            OnPropertyChanged(nameof(SelectedProviderName));
            OnPropertyChanged(nameof(CurrentApiKey));
            OnPropertyChanged(nameof(CurrentSettings));
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                IsSaving = true;

                // Save API keys
                foreach (var kvp in _apiKeys)
                {
                    await _settingsService.SaveApiKeyAsync(kvp.Key, kvp.Value);
                }

                // Save provider-specific settings
                foreach (var kvp in _providerSettings)
                {
                    var settingsKey = $"{kvp.Key}_settings";
                    string settingsJson = System.Text.Json.JsonSerializer.Serialize(kvp.Value);
                    await _settingsService.SaveSettingAsync(settingsKey, settingsJson);
                }

                // Show success message
                var dialog = new ContentDialog
                {
                    Title = "Instellingen opgeslagen",
                    Content = "Alle instellingen zijn succesvol opgeslagen.",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                // Show error message
                var dialog = new ContentDialog
                {
                    Title = "Fout bij opslaan",
                    Content = $"Er is een fout opgetreden bij het opslaan van de instellingen: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            finally
            {
                IsSaving = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
