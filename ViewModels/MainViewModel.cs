using DJAI.Commands;
using DJAI.Contracts;
using DJAI.Helpers;
using DJAI.Models;
using DJAI.Services;
using DJAI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

#nullable enable

namespace DJAI.ViewModels
{
    public enum PageType
    {
        Chat,
        Settings
    }

    public partial class MainViewModel : INotifyPropertyChanged
    {
        private readonly AIServiceFactory _serviceFactory;
        private readonly ApiRateLimiter _rateLimiter;
        private readonly MessageLimitHandler _messageLimitHandler;
        private readonly ICacheService _cacheService;
        private readonly SettingsService _settingsService;
        private readonly ExportService _exportService;

        // Initialize with non-null values or mark as nullable with ?
        private ChatViewModel? _chatViewModel;
        private UIElement? _activePage;
        private Conversation? _selectedConversation;
        private bool _isExportMenuOpen;
        private AIProvider _selectedProvider;
        private ObservableCollection<Conversation> _conversations = [];

        public ChatViewModel? ChatViewModel
        {
            get => _chatViewModel;
            private set
            {
                if (_chatViewModel != value)
                {
                    _chatViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        public UIElement? ActivePage
        {
            get => _activePage;
            private set
            {
                if (_activePage != value)
                {
                    _activePage = value;
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
                    UpdateAIService();
                }
            }
        }

        public ObservableCollection<Conversation> Conversations
        {
            get => _conversations;
            set
            {
                if (_conversations != value)
                {
                    _conversations = value;
                    OnPropertyChanged();
                }
            }
        }

        public Conversation? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                if (_selectedConversation != value)
                {
                    _selectedConversation = value;
                    OnPropertyChanged();
                    LoadSelectedConversation();
                }
            }
        }

        public bool IsExportMenuOpen
        {
            get => _isExportMenuOpen;
            set
            {
                if (_isExportMenuOpen != value)
                {
                    _isExportMenuOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand NewConversationCommand { get; }
        public ICommand DeleteConversationCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand CloseSettingsCommand { get; }
        public ICommand ExportConversationCommand { get; }
        public ICommand CloseExportMenuCommand { get; }
        public ICommand ExportAsCommand { get; }

        public MainViewModel(
            AIServiceFactory serviceFactory,
            ApiRateLimiter rateLimiter,
            MessageLimitHandler messageLimitHandler,
            ICacheService cacheService,
            SettingsService settingsService,
            ExportService exportService)
        {
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _messageLimitHandler = messageLimitHandler ?? throw new ArgumentNullException(nameof(messageLimitHandler));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            _selectedProvider = AIProvider.Anthropic;

            // Initialize commands
            NewConversationCommand = new RelayCommand(_ => CreateNewConversation());
            DeleteConversationCommand = new RelayCommand(_ => DeleteSelectedConversation(), _ => SelectedConversation != null);
            OpenSettingsCommand = new RelayCommand(_ => NavigateTo(PageType.Settings));
            CloseSettingsCommand = new RelayCommand(_ => NavigateTo(PageType.Chat));
            ExportConversationCommand = new RelayCommand(_ => IsExportMenuOpen = true, _ => SelectedConversation != null);
            CloseExportMenuCommand = new RelayCommand(_ => IsExportMenuOpen = false);
            ExportAsCommand = new RelayCommand(async format => await ExportConversationAsync(format as string ?? string.Empty));

            // Initialize the UI
            UpdateAIService();
            NavigateTo(PageType.Chat);
            LoadConversationsAsync();
        }

        private void NavigateTo(PageType pageType)
        {
            // Afhankelijk van pageType, toon juiste pagina
            switch (pageType)
            {
                case PageType.Chat:
                    if (ChatViewModel != null)
                    {
                        ActivePage = new ChatPage { DataContext = ChatViewModel };
                    }
                    break;
                case PageType.Settings:
                    ActivePage = new SettingsPage { DataContext = new SettingsViewModel(_settingsService) };
                    break;
            }
        }

        private async void LoadConversationsAsync()
        {
            // Laad gesprekken uit de cache
            try
            {
                var conversationIds = await _cacheService.GetAsync<string[]>("saved_conversation_ids");
                if (conversationIds != null && conversationIds.Length > 0)
                {
                    foreach (var id in conversationIds)
                    {
                        var conversation = await _cacheService.GetAsync<Conversation>($"conversation_{id}");
                        if (conversation != null)
                        {
                            Conversations.Add(conversation);
                        }
                    }

                    if (Conversations.Any())
                    {
                        SelectedConversation = Conversations.First();
                    }
                    else
                    {
                        CreateNewConversation();
                    }
                }
                else
                {
                    CreateNewConversation();
                }
            }
            catch (Exception)
            {
                // Indien er een fout is, maak een nieuw gesprek aan
                CreateNewConversation();
            }
        }

        private void CreateNewConversation()
        {
            var newConversation = new Conversation
            {
                Title = $"Nieuw gesprek {DateTime.Now:g}",
                SelectedProvider = SelectedProvider.ToString()
            };

            Conversations.Add(newConversation);
            SelectedConversation = newConversation;
            _ = SaveConversationsAsync();
        }

        public void DeleteSelectedConversation()
        {
            if (SelectedConversation == null)
                return;

            var idToRemove = SelectedConversation.Id;
            Conversations.Remove(SelectedConversation);

            // Verwijder uit cache
            _cacheService.RemoveAsync($"conversation_{idToRemove}");

            if (Conversations.Any())
            {
                SelectedConversation = Conversations.First();
            }
            else
            {
                CreateNewConversation();
            }

            _ = SaveConversationsAsync();
        }

        private void LoadSelectedConversation()
        {
            if (SelectedConversation == null)
                return;

            // Bepaal de provider
            if (Enum.TryParse<AIProvider>(SelectedConversation.SelectedProvider, out var provider))
            {
                SelectedProvider = provider;
            }

            // Update de chatviewmodel met geselecteerde gesprek
            UpdateAIService();
        }

        private void UpdateAIService()
        {
            try
            {
                var aiService = _serviceFactory.CreateService(SelectedProvider);
                ChatViewModel = new ChatViewModel(aiService, _rateLimiter, _messageLimitHandler, _cacheService, SelectedConversation);

                // Update het geselecteerde gesprek met de huidige provider
                if (SelectedConversation != null)
                {
                    SelectedConversation.SelectedProvider = SelectedProvider.ToString();
                    _ = SaveConversationsAsync();
                }

                // Als we op de chat pagina zijn, update de actieve pagina
                if (ActivePage is ChatPage)
                {
                    ActivePage = new ChatPage { DataContext = ChatViewModel };
                }
            }
            catch (Exception ex)
            {
                // Toon foutmelding aan gebruiker
                _ = ShowErrorDialogAsync("Fout bij wijzigen AI provider", ex.Message);
            }
        }

        private async Task ExportConversationAsync(string formatString)
        {
            IsExportMenuOpen = false;

            if (SelectedConversation == null || string.IsNullOrEmpty(formatString))
                return;

            if (Enum.TryParse<ExportService.ExportFormat>(formatString, out var format))
            {
                bool success = await _exportService.ExportConversationAsync(SelectedConversation, format);

                if (!success)
                {
                    await ShowErrorDialogAsync("Export Mislukt", "Het gesprek kon niet worden geëxporteerd.");
                }
            }
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };

            await dialog.ShowAsync();
        }

        private async Task SaveConversationsAsync()
        {
            try
            {
                // Sla conversatie ID's op
                var ids = Conversations.Select(c => c.Id).ToArray();
                await _cacheService.SetAsync("saved_conversation_ids", ids);

                // Sla elke conversatie op
                foreach (var conversation in Conversations)
                {
                    await _cacheService.SetAsync($"conversation_{conversation.Id}", conversation);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fout bij opslaan van gesprekken: {ex.Message}");
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}