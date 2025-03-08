using DJAI.Commands;
using DJAI.Contracts;
using DJAI.Helpers;
using DJAI.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

#nullable enable

namespace DJAI.ViewModels
{
    public partial class ChatViewModel : INotifyPropertyChanged
    {
        private readonly IAIService _aiService;
        private readonly ApiRateLimiter _rateLimiter;
        private readonly MessageLimitHandler _messageLimitHandler;
        private readonly ICacheService _cacheService;

        private string _userInput = string.Empty;
        private bool _isGenerating;
        private string _selectedProvider;
        private Conversation _currentConversation;

        public ObservableCollection<ChatMessage> Messages { get; } = [];

        public string UserInput
        {
            get => _userInput;
            set
            {
                if (_userInput != value)
                {
                    _userInput = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                if (_isGenerating != value)
                {
                    _isGenerating = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (_selectedProvider != value)
                {
                    _selectedProvider = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AIServiceName => _aiService?.ProviderName ?? "AI Service";
        public string CurrentModelName => "Default Model";

        public ICommand SendMessageCommand { get; }
        public ICommand ClearConversationCommand { get; }

        public ChatViewModel(
            IAIService aiService,
            ApiRateLimiter rateLimiter,
            MessageLimitHandler messageLimitHandler,
            ICacheService cacheService)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _messageLimitHandler = messageLimitHandler ?? throw new ArgumentNullException(nameof(messageLimitHandler));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

            _currentConversation = new Conversation();
            _selectedProvider = _aiService.ProviderName;

            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync());
            ClearConversationCommand = new RelayCommand(_ => ClearConversation());
        }

        // Overload that accepts an existing conversation
        public ChatViewModel(
            IAIService aiService,
            ApiRateLimiter rateLimiter,
            MessageLimitHandler messageLimitHandler,
            ICacheService cacheService,
            Conversation? conversation = null)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _messageLimitHandler = messageLimitHandler ?? throw new ArgumentNullException(nameof(messageLimitHandler));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

            _currentConversation = conversation ?? new Conversation();
            _selectedProvider = _aiService.ProviderName;

            // Load conversation messages if provided
            if (conversation != null && conversation.Messages.Count > 0)
            {
                foreach (var message in conversation.Messages)
                {
                    Messages.Add(message);
                }
            }

            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync());
            ClearConversationCommand = new RelayCommand(_ => ClearConversation());
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserInput) || IsGenerating)
                return;

            IsGenerating = true;

            try
            {
                var userMessage = new ChatMessage
                {
                    Role = MessageRole.User,
                    Content = UserInput,
                    ConversationId = _currentConversation.Id
                };

                Messages.Add(userMessage);
                _currentConversation.Messages.Add(userMessage);

                // Wacht op rate limiter
                await _rateLimiter.AcquireAsync(_selectedProvider);

                var assistantMessage = new ChatMessage
                {
                    Role = MessageRole.Assistant,
                    Content = "Genereren van antwoord...",
                    IsComplete = false,
                    ConversationId = _currentConversation.Id
                };

                Messages.Add(assistantMessage);

                // Vraag antwoord op van AI service
                var response = await _aiService.GetCompletionAsync(
                    UserInput,
                    _currentConversation.Messages);

                // Behandel message limit als nodig
                if (response.ReachedTokenLimit)
                {
                    response = await _messageLimitHandler.HandleMessageLimitAsync(
                        _aiService,
                        response);
                }

                // Update het assistantbericht met de ontvangen respons
                assistantMessage.Content = response.Text;
                assistantMessage.IsComplete = response.IsComplete;

                // Update de assistantMessage in ObservableCollection
                var index = Messages.IndexOf(assistantMessage);
                Messages[index] = assistantMessage;

                // Cache conversation
                _currentConversation.Messages.Add(assistantMessage);
                _currentConversation.LastUpdatedAt = DateTime.Now;
                await _cacheService.SetAsync($"conversation_{_currentConversation.Id}", _currentConversation);

                UserInput = string.Empty;
            }
            catch (Exception ex)
            {
                // Log error en toon in interface
                var errorMessage = new ChatMessage
                {
                    Role = MessageRole.System,
                    Content = $"Fout: {ex.Message}",
                    ConversationId = _currentConversation.Id
                };
                Messages.Add(errorMessage);
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private void ClearConversation()
        {
            Messages.Clear();
            _currentConversation = new Conversation();
            UserInput = string.Empty;
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