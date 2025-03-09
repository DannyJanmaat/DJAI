// Create file: ViewModels/MainViewModel.cs
using DJAI.Models;
using DJAI.Services;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DJAI.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        // Services
        private readonly ILLMService? _llmService;
        private readonly DispatcherQueue? _dispatcherQueue;

        // Cancellation token for API requests
        private CancellationTokenSource? _cancellationTokenSource;

        // State
        private bool _isGenerating;
        private string _userInput = string.Empty;
        private string _statusMessage = "Ready";

        /// <summary>
        /// Event that is raised when a property changes
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Collection of messages in the chat
        /// </summary>
        public ObservableCollection<Message> Messages { get; } = [];

        /// <summary>
        /// User input in the text box
        /// </summary>
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

        /// <summary>
        /// Status message displayed in the UI
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Indicates whether text is currently being generated
        /// </summary>
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

        /// <summary>
        /// Command to send a message
        /// </summary>
        public ICommand SendMessageCommand { get; }

        /// <summary>
        /// Command to cancel message generation
        /// </summary>
        public ICommand CancelGenerationCommand { get; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class
        /// </summary>
        public MainViewModel(ILLMService? llmService = null)
        {
            _llmService = llmService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Initialize commands using simple command implementation
            SendMessageCommand = new SimpleCommand(async _ => await SendMessageAsync(), _ => !IsGenerating && !string.IsNullOrWhiteSpace(UserInput));
            CancelGenerationCommand = new SimpleCommand(_ => CancelGeneration(), _ => IsGenerating);

            // Add welcome message
            AddWelcomeMessage();
        }

        /// <summary>
        /// Adds a welcome message to the chat
        /// </summary>
        private void AddWelcomeMessage()
        {
            Message welcomeMessage = Message.FromSystem(
                "Welcome to DJAI! I'm an AI assistant that can help you with various questions. " +
                "Type a message to get started.");

            Messages.Add(welcomeMessage);
        }

        /// <summary>
        /// Sends a message and generates a response
        /// </summary>
        private async Task SendMessageAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UserInput) || IsGenerating)
                {
                    return;
                }

                if (_llmService == null || !_llmService.IsConfigured())
                {
                    StatusMessage = "AI service is not configured";
                    return;
                }

                // Create a new user message
                Message userMessage = Message.FromUser(UserInput);
                Messages.Add(userMessage);

                // Clear the user input
                UserInput = string.Empty;

                // Create a placeholder for the AI response
                Message aiMessage = Message.FromAssistant("...");
                Messages.Add(aiMessage);

                // Set the status
                IsGenerating = true;
                StatusMessage = "Generating...";

                // Create a new cancellation token source
                _cancellationTokenSource = new CancellationTokenSource();

                // Generate a response
                string response;

                try
                {
                    // For testing, use a simulated response
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                    response = $"This is a simulated response to: {userMessage.Content}";

                    // In a real implementation, you would use:
                    // var messages = new List<Message>(Messages);
                    // response = await _llmService.GetCompletionAsync(messages, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    response = "[Generation cancelled]";
                }

                // Update the AI message with the response
                _ = (_dispatcherQueue?.TryEnqueue(() =>
                {
                    aiMessage.Content = response;
                    OnPropertyChanged(nameof(Messages));
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SendMessageAsync: {ex.Message}");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                // Reset the status
                IsGenerating = false;
                StatusMessage = "Ready";
            }
        }

        /// <summary>
        /// Cancels the ongoing text generation
        /// </summary>
        private void CancelGeneration()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Simple implementation of ICommand
    /// </summary>
    public partial class SimpleCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<object?, bool>? _canExecute = canExecute;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
