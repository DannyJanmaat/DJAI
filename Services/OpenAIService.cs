using DJAI.Contracts;
using DJAI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DJAI.Services
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _cacheService;
        private readonly string _apiKey;
        private readonly string _model;

        public string ProviderName => "OpenAI";

        public OpenAIService(ICacheService cacheService, string apiKey, string model = "gpt-4o")
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.openai.com/v1/")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model ?? "gpt-4o";
        }

        public async Task<AIResponse> GetCompletionAsync(string prompt, IEnumerable<ChatMessage> conversation, CancellationToken cancellationToken = default)
        {
            try
            {
                // Converteer het gesprek naar het OpenAI chat format  
                List<object> messages = [];
                foreach (ChatMessage message in conversation)
                {
                    string role = message.Role switch
                    {
                        MessageRole.User => "user",
                        MessageRole.Assistant => "assistant",
                        MessageRole.System => "system",
                        _ => "user"
                    };

                    messages.Add(new
                    {
                        role,
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
                    model = _model,
                    messages,
                    max_tokens = 2000,
                    temperature = 0.7
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
                await _cacheService.SetAsync($"openai_completion_{conversationId}", responseObject.ToString(), TimeSpan.FromHours(1));

                bool isComplete = true;
                string assistantResponse = string.Empty;

                // Parse the OpenAI response structure  
                if (responseObject.TryGetProperty("choices", out JsonElement choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out JsonElement responseMessage) &&
                    responseMessage.TryGetProperty("content", out JsonElement content_text))
                {
                    assistantResponse = content_text.GetString() ?? string.Empty;
                }

                if (responseObject.TryGetProperty("choices", out JsonElement choicesArr) &&
                    choicesArr.GetArrayLength() > 0 &&
                    choicesArr[0].TryGetProperty("finish_reason", out JsonElement finishReason))
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
                    Text = $"Fout bij OpenAI API aanroep: {ex.Message}",
                    IsComplete = false
                };
            }
        }

        public async Task<AIResponse> ContinueGenerationAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                string cachedResponse = await _cacheService.GetAsync<string>($"openai_completion_{conversationId}") ?? string.Empty;
                if (string.IsNullOrEmpty(cachedResponse))
                {
                    return new AIResponse
                    {
                        Text = "Kan generatie niet voortzetten. Gespreksgegevens niet gevonden.",
                        IsComplete = true
                    };
                }

                JsonElement responseObject = JsonSerializer.Deserialize<JsonElement>(cachedResponse);

                // Haal de laatste assistant message op
                string lastAssistantMessage = string.Empty;
                if (responseObject.TryGetProperty("choices", out JsonElement choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out JsonElement message) &&
                    message.TryGetProperty("content", out JsonElement content_text))
                {
                    lastAssistantMessage = content_text.GetString() ?? string.Empty;
                }

                // Stuur een nieuw verzoek met instructie om door te gaan
                var requestContent = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = "Je moet je vorige antwoord verder aanvullen." },
                        new { role = "assistant", content = lastAssistantMessage },
                        new { role = "user", content = "Ga verder waar je gebleven was." }
                    },
                    max_tokens = 2000,
                    temperature = 0.7
                };

                string jsonContent = JsonSerializer.Serialize(requestContent);
                StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                string nextResponseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                JsonElement nextResponseObject = JsonSerializer.Deserialize<JsonElement>(nextResponseJson);

                bool isComplete = true;
                string continuedResponse = string.Empty;

                if (nextResponseObject.TryGetProperty("choices", out JsonElement nextChoices) &&
                    nextChoices.GetArrayLength() > 0 &&
                    nextChoices[0].TryGetProperty("message", out JsonElement nextMessage) &&
                    nextMessage.TryGetProperty("content", out JsonElement nextContent))
                {
                    continuedResponse = nextContent.GetString() ?? string.Empty;
                }

                if (nextResponseObject.TryGetProperty("choices", out JsonElement nextChoicesArr) &&
                    nextChoicesArr.GetArrayLength() > 0 &&
                    nextChoicesArr[0].TryGetProperty("finish_reason", out JsonElement finishReason))
                {
                    isComplete = finishReason.GetString() != "length";
                }

                // Cache de bijgewerkte respons
                await _cacheService.SetAsync($"openai_completion_{conversationId}_continued", nextResponseObject.ToString(), TimeSpan.FromHours(1));

                return new AIResponse
                {
                    Text = continuedResponse,
                    IsComplete = isComplete,
                    ConversationId = conversationId,
                    ReachedTokenLimit = !isComplete,
                    ReachedRateLimit = false
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