using DJAI.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DJAI.Contracts
{
    public interface IAIService
    {
        string ProviderName { get; }
        Task<AIResponse> GetCompletionAsync(string prompt, IEnumerable<ChatMessage> conversation, CancellationToken cancellationToken = default);
        Task<AIResponse> ContinueGenerationAsync(string conversationId, CancellationToken cancellationToken = default);
    }
}