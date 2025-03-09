using DJAI.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DJAI.Services
{
    public interface ILLMService
    {
        Task<string> GetCompletionAsync(List<Message> messages, CancellationToken cancellationToken = default);
        Task<string> GetStreamingCompletionAsync(List<Message> messages, Action<string> onPartialResponse, CancellationToken cancellationToken = default);
        Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
        void SetModel(string modelName);
        void SetApiKey(string apiKey);
        string GetCurrentModel();
        bool IsConfigured();
        int GetModelTokenLimit();
    }
}
