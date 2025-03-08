using DJAI.Contracts;
using DJAI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DJAI.Services
{
    public class GitHubCopilotService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly string _apiKey;

        public string ProviderName => "GitHubCopilot";

        public GitHubCopilotService(ICacheService cacheService, string apiKey)
        {
            _httpClient = new HttpClient
            {
                // Opmerking: Dit is een hypothetisch endpoint omdat GitHub Copilot API nog in ontwikkeling is
                BaseAddress = new Uri("https://api.githubcopilot.com/")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _cacheService = cacheService;
            _apiKey = apiKey;
        }

        public async Task<AIResponse> GetCompletionAsync(string prompt, IEnumerable<ChatMessage> conversation, CancellationToken cancellationToken = default)
        {
            try
            {
                // Converteer gesprek naar een format dat GitHub Copilot kan begrijpen
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
                    model = "copilot-chat",
                    messages,
                    temperature = 0.7,
                    max_tokens = 2000
                };

                string jsonContent = JsonSerializer.Serialize(requestContent);
                StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                JsonElement responseObject = JsonSerializer.Deserialize<JsonElement>(responseJson);

                string conversationId = Guid.NewGuid().ToString();
                string messageId = Guid.NewGuid().ToString();

                // Cache the response for continuing generation if needed
                await _cacheService.SetAsync($"github_copilot_completion_{conversationId}", responseObject.ToString(), TimeSpan.FromHours(1));

                bool isComplete = true;
                string assistantResponse = string.Empty;

                // Parse de response op basis van de Copilot API structuur
                // Note: Deze implementatie is hypothetisch en moet worden aangepast wanneer de echte API bekend is
                if (responseObject.TryGetProperty("choices", out JsonElement choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out JsonElement responseMessage) &&
                    responseMessage.TryGetProperty("content", out JsonElement content_text))
                {
                    assistantResponse = content_text.GetString() ?? string.Empty;
                }

                if (responseObject.TryGetProperty("finish_reason", out JsonElement finishReason))
                {
                    isComplete = finishReason.GetString() != "length";
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
                    Text = $"Fout bij GitHub Copilot API aanroep: {ex.Message}",
                    IsComplete = false
                };
            }
        }

        public async Task<AIResponse> ContinueGenerationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                string cachedResponse = await _cacheService.GetAsync<string>($"github_copilot_completion_{conversationId}") ?? string.Empty;
                if (string.IsNullOrEmpty(cachedResponse))
                {
                    return new AIResponse
                    {
                        Text = "Kan generatie niet voortzetten. Gespreksgegevens niet gevonden.",
                        IsComplete = true
                    };
                }

                // Implementatie voor het vervolgen van generatie
                // Hier zou je de logica moeten implementeren om de generatie voort te zetten
                // Dit is afhankelijk van de API-specificaties van GitHub Copilot

                return new AIResponse
                {
                    Text = "Voortzetting van generatie is nog niet geïmplementeerd voor GitHub Copilot.",
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
