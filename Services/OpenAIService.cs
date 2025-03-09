using Azure;
using Azure.AI.OpenAI;
using DJAI.Models;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DJAI.Services
{
    public class OpenAIService : ILLMService
    {
        private string _apiKey = string.Empty;
        private string _model = "gpt-3.5-turbo";
        private OpenAIClient? _client;

        public OpenAIService(string apiKey = "", string model = "gpt-3.5-turbo")
        {
            _apiKey = apiKey;
            _model = model;

            if (!string.IsNullOrEmpty(apiKey))
            {
                _client = new OpenAIClient(new AzureKeyCredential(apiKey));
            }
        }

        public async Task<string> GetCompletionAsync(List<Message> messages, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured())
            {
                throw new InvalidOperationException("OpenAI service is not configured. Set an API key first.");
            }

            try
            {
                if (_client == null)
                {
                    throw new InvalidOperationException("OpenAI client is not initialized.");
                }

                // Convert messages to Azure OpenAI format
                var chatCompletionsOptions = new ChatCompletionsOptions
                {
                    Temperature = 0.7f,
                    MaxTokens = 2048,
                    DeploymentName = _model
                };

                // Add messages to options
                foreach (var message in messages)
                {
                    ChatRole role = message.Role switch
                    {
                        "assistant" => ChatRole.Assistant,
                        "system" => ChatRole.System,
                        _ => ChatRole.User
                    };

                    chatCompletionsOptions.Messages.Add(new ChatMessage(role, message.Content));
                }

                // Send request to API
                var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions, cancellationToken);

                // Return response
                return response.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OpenAI API call: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetStreamingCompletionAsync(List<Message> messages, Action<string> onPartialResponse, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured())
            {
                throw new InvalidOperationException("OpenAI service is not configured. Set an API key first.");
            }

            try
            {
                if (_client == null)
                {
                    throw new InvalidOperationException("OpenAI client is not initialized.");
                }

                // Create chat completion options
                var chatCompletionsOptions = new ChatCompletionsOptions
                {
                    Temperature = 0.7f,
                    MaxTokens = 2048,
                    DeploymentName = _model
                };

                // Add messages to options
                foreach (var message in messages)
                {
                    ChatRole role = message.Role switch
                    {
                        "assistant" => ChatRole.Assistant,
                        "system" => ChatRole.System,
                        _ => ChatRole.User
                    };

                    chatCompletionsOptions.Messages.Add(new ChatMessage(role, message.Content));
                }

                // Send streaming request to API
                var streamingResponse = await _client.GetChatCompletionsStreamingAsync(chatCompletionsOptions, cancellationToken);

                var fullResponse = new StringBuilder();
                await foreach (var update in streamingResponse.WithCancellation(cancellationToken))
                {
                    if (update.ContentUpdate != null)
                    {
                        fullResponse.Append(update.ContentUpdate);
                        onPartialResponse(fullResponse.ToString());
                    }
                }

                return fullResponse.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OpenAI streaming API call: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            // Default list of supported models
            var defaultModels = new List<string>
            {
                "gpt-3.5-turbo",
                "gpt-3.5-turbo-16k",
                "gpt-4",
                "gpt-4-32k",
                "gpt-4-turbo"
            };

            // In a full implementation, you would call the API to get available models
            // For now, return the default list
            return await Task.FromResult(defaultModels);
        }

        public void SetModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ArgumentException("Model name cannot be empty", nameof(modelName));
            }

            _model = modelName;
        }

        public void SetApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API key cannot be empty", nameof(apiKey));
            }

            _apiKey = apiKey;

            // Create a new client with the new API key
            _client = new OpenAIClient(new AzureKeyCredential(apiKey));
        }

        public string GetCurrentModel()
        {
            return _model;
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_model) && _client != null;
        }

        public int GetModelTokenLimit()
        {
            // Return token limits based on model
            return _model switch
            {
                "gpt-3.5-turbo" => 4096,
                "gpt-3.5-turbo-16k" => 16384,
                "gpt-4" => 8192,
                "gpt-4-32k" => 32768,
                "gpt-4-turbo" => 128000,
                _ => 4096 // Default
            };
        }
    }
}
