using DJAI.Models;
using System.Text.Json;
using Windows.Storage;

namespace DJAI.Services
{
    public class SettingsService
    {
        private const string SettingsFileName = "settings.json";
        private readonly Dictionary<string, string> _settings;

        public SettingsService()
        {
            _settings = LoadSettingsSync();
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            return _settings.TryGetValue(key, out string? value) ? value : defaultValue;
        }

        public T GetSetting<T>(string key, T defaultValue = default) where T : struct
        {
            if (_settings.TryGetValue(key, out string? stringValue))
            {
                try
                {
                    T? deserializedValue = JsonSerializer.Deserialize<T>(stringValue);
                    return deserializedValue ?? defaultValue;
                }
                catch
                {
                    // Als deserialisatie mislukt, gebruik standaardwaarde
                }
            }
            return defaultValue;
        }

        public async Task SaveSettingAsync(string key, string value)
        {
            _settings[key] = value;
            await SaveSettingsAsync();
        }

        public async Task SaveSettingAsync<T>(string key, T value)
        {
            string jsonValue = JsonSerializer.Serialize(value);
            _settings[key] = jsonValue;
            await SaveSettingsAsync();
        }

        public string GetApiKey(AIProvider provider)
        {
            return GetSetting(provider.ToApiKeySettingName());
        }

        public async Task SaveApiKeyAsync(AIProvider provider, string apiKey)
        {
            await SaveSettingAsync(provider.ToApiKeySettingName(), apiKey);
        }

        private static Dictionary<string, string> LoadSettingsSync()
        {
            try
            {
                string settingsPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, SettingsFileName);
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij laden instellingen: {ex.Message}");
            }

            return [];
        }

        private static async Task<Dictionary<string, string>> LoadSettingsAsync()
        {
            try
            {
                StorageFile? file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(SettingsFileName) as StorageFile;
                if (file != null)
                {
                    string json = await FileIO.ReadTextAsync(file);
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij laden instellingen: {ex.Message}");
            }

            return [];
        }


        private async Task SaveSettingsAsync()
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings);
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    SettingsFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij opslaan instellingen: {ex.Message}");
            }
        }
    }
}