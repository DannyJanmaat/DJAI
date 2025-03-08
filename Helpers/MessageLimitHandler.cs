using DJAI.Contracts;
using DJAI.Models;

namespace DJAI.Helpers
{
    public class MessageLimitHandler(ICacheService cacheService)
    {
        private readonly ICacheService _cacheService = cacheService;

        public async Task<AIResponse> HandleMessageLimitAsync(
            IAIService aiService,
            AIResponse initialResponse,
            CancellationToken cancellationToken = default)
        {
            if (initialResponse.IsComplete || initialResponse.ReachedRateLimit)
            {
                return initialResponse;
            }

            // Als we de token limiet hebben bereikt, probeer verder te genereren
            if (initialResponse.ReachedTokenLimit)
            {
                AIResponse continuedResponse = await aiService.ContinueGenerationAsync(
                    initialResponse.ConversationId,
                    cancellationToken);

                // Combineer de initiële respons met de vervolgrespons
                initialResponse.Text += continuedResponse.Text;
                initialResponse.IsComplete = continuedResponse.IsComplete;
                initialResponse.ReachedTokenLimit = continuedResponse.ReachedTokenLimit;

                // Recursief doorgaan totdat we een volledige respons hebben
                if (!continuedResponse.IsComplete && continuedResponse.ReachedTokenLimit)
                {
                    return await HandleMessageLimitAsync(aiService, initialResponse, cancellationToken);
                }
            }

            return initialResponse;
        }
    }
}