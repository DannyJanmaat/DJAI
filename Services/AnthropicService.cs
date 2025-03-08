using DJAI.Contracts;
using DJAI.Models;
using System.Text;
using System.Text.Json;

namespace DJAI.Services
{
    public class AnthropicService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly string _apiKey;

        public string ProviderName => "Anthropic";

        public AnthropicService(ICacheService cacheService, string apiKey)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.anthropic.com/v1/")
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            _cacheService = cacheService;
            _apiKey = apiKey;
        }

        public async Task<AIResponse> GetCompletionAsync(string prompt, IEnumerable<ChatMessage> conversation, CancellationToken cancellationToken = default)
        {
            try
            {
                // Converteer het gesprek naar het Anthropic formaat
                List<object> messages = [];
                foreach (ChatMessage message in conversation)
                {
                    messages.Add(new
                    {
                        role = message.Role.ToString().ToLower(),
                        content = message.Content
                    });
                }

                // Voeg de nieuwe gebruikersvraag toe
                messages.Add(new
                {
                    role = "user",
                    content = prompt
                });

                var requestContent = new
                {
                    model = "claude-3-opus-20240229",
                    messages,
                    max_tokens = 4000
                };

                string jsonContent = JsonSerializer.Serialize(requestContent);
                StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("messages", content, cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                JsonElement responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

                string conversationId = Guid.NewGuid().ToString();
                string messageId = Guid.NewGuid().ToString();

                // Cache the response for continuing generation if needed
                await _cacheService.SetAsync($"anthropic_completion_{conversationId}", responseObject.ToString(), TimeSpan.FromHours(1));

                bool isComplete = true;
                if (responseObject.TryGetProperty("stop_reason", out JsonElement stopReason))
                {
                    isComplete = stopReason.GetString() != "max_tokens";
                }

                string assistantResponse = string.Empty;
                if (responseObject.TryGetProperty("content", out JsonElement content_array))
                {
                    foreach (JsonElement item in content_array.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out JsonElement type) && type.GetString() == "text")
                        {
                            if (item.TryGetProperty("text", out JsonElement text))
                            {
                                assistantResponse = text.GetString() ?? string.Empty;
                            }
                        }
                    }
                }

                return new AIResponse
                {
                    Text = assistantResponse,
                    IsComplete = isComplete,
                    ConversationId = conversationId,
                    MessageId = messageId,
                    ReachedTokenLimit = !isComplete,
                    ReachedRateLimit = false
                };
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                return new AIResponse
                {
                    Text = "Rate limit bereikt. Probeer het later opnieuw.",
                    IsComplete = false,
                    ReachedRateLimit = true
                };
            }
            catch (Exception ex)
            {
                return new AIResponse
                {
                    Text = $"Fout: {ex.Message}",
                    IsComplete = false
                };
            }
        }

        public async Task<AIResponse> ContinueGenerationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            // Implementatie voor het voortzetten van generatie
            // Dit zou specifiek voor Anthropic moeten worden geïmplementeerd
            try
            {
                string cachedResponse = await _cacheService.GetAsync<string>($"anthropic_completion_{conversationId}") ?? string.Empty;
                if (string.IsNullOrEmpty(cachedResponse))
                {
                    return new AIResponse
                    {
                        Text = "Kan generatie niet voortzetten. Gespreksgegevens niet gevonden.",
                        IsComplete = true
                    };
                }

                // TODO: Implementeer logica voor voortzetten van generatie

                return new AIResponse
                {
                    Text = "Vervolgfunctionaliteit nog niet geïmplementeerd voor Anthropic.",
                    IsComplete = true,
                    ConversationId = conversationId
                };
            }
            catch (Exception ex)
            {
                return new AIResponse
                {
                    Text = $"Fout bij voortzetten generatie: {ex.Message}",
                    IsComplete = false
                };
            }
        }
    }
}