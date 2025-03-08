namespace DJAI.Models
{
    public enum AIProvider
    {
        Anthropic,
        OpenAI,
        GitHubCopilot
    }

    public static class AIProviderExtensions
    {
        public static string ToDisplayName(this AIProvider provider)
        {
            return provider switch
            {
                AIProvider.Anthropic => "Anthropic Claude",
                AIProvider.OpenAI => "OpenAI GPT",
                AIProvider.GitHubCopilot => "GitHub Copilot",
                _ => provider.ToString()
            };
        }

        public static string ToApiKeySettingName(this AIProvider provider)
        {
            return provider switch
            {
                AIProvider.Anthropic => "AnthropicApiKey",
                AIProvider.OpenAI => "OpenAIApiKey",
                AIProvider.GitHubCopilot => "GitHubCopilotApiKey",
                _ => string.Empty
            };
        }

        public static string GetDefaultModel(this AIProvider provider)
        {
            return provider switch
            {
                AIProvider.Anthropic => "claude-3-opus-20240229",
                AIProvider.OpenAI => "gpt-4o",
                AIProvider.GitHubCopilot => "copilot-chat",
                _ => string.Empty
            };
        }
    }
}