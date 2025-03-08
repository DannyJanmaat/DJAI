using DJAI.Contracts;
using DJAI.Models;
using Microsoft.Extensions.Configuration;

namespace DJAI.Services
{
    public class AIServiceFactory(ICacheService cacheService, IConfiguration configuration)
    {
        private readonly ICacheService _cacheService = cacheService;
        private readonly IConfiguration _configuration = configuration;

        public IAIService CreateService(AIProvider provider)
        {
            string? apiKey = _configuration[provider.ToApiKeySettingName()];

            return string.IsNullOrEmpty(apiKey)
                ? throw new InvalidOperationException($"API Key niet geconfigureerd voor {provider.ToDisplayName()}")
                : provider switch
                {
                    AIProvider.Anthropic => new AnthropicService(_cacheService, apiKey),
                    AIProvider.OpenAI => new OpenAIService(_cacheService, apiKey),
                    AIProvider.GitHubCopilot => new GitHubCopilotService(_cacheService, apiKey),
                    _ => throw new ArgumentException($"Niet-ondersteunde AI provider: {provider}")
                };
        }
    }
}