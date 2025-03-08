namespace DJAI.Models
{
    public class AIProviderSettings
    {
        public AIProvider Provider { get; set; }
        public string DefaultModel { get; set; } = string.Empty;
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2000;
        public bool StreamingEnabled { get; set; } = true;
        public int MaxContextLength { get; set; } = 8000;
        public List<string> AvailableModels { get; set; } = [];

        public AIProviderSettings()
        {
            // Initialize with default values based on provider
            SetDefaultsForProvider();
        }

        private void SetDefaultsForProvider()
        {
            switch (Provider)
            {
                case AIProvider.Anthropic:
                    DefaultModel = "claude-3-opus-20240229";
                    AvailableModels =
                    [
                        "claude-3-opus-20240229",
                        "claude-3-sonnet-20240229",
                        "claude-3-haiku-20240307"
                    ];
                    MaxContextLength = 100000;
                    break;

                case AIProvider.OpenAI:
                    DefaultModel = "gpt-4o";
                    AvailableModels =
                    [
                        "gpt-4o",
                        "gpt-4-turbo",
                        "gpt-3.5-turbo"
                    ];
                    MaxContextLength = 128000;
                    break;

                case AIProvider.GitHubCopilot:
                    DefaultModel = "copilot-chat";
                    AvailableModels = ["copilot-chat"];
                    MaxContextLength = 8192;
                    break;
            }
        }
    }
}