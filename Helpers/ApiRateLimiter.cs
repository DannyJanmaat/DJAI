using System.Collections.Concurrent;

namespace DJAI.Helpers
{
    public class ApiRateLimiter
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _providerSemaphores = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastRequestTimes = new();
        private readonly ConcurrentDictionary<string, int> _requestsPerMinute = new();

        public ApiRateLimiter()
        {
            // Configureer standaard rate limits voor providers
            _requestsPerMinute["Anthropic"] = 10;  // Voorbeeld: 10 requests per minuut
            _requestsPerMinute["OpenAI"] = 20;     // Voorbeeld: 20 requests per minuut
            _requestsPerMinute["GitHubCopilot"] = 15; // Voorbeeld: 15 requests per minuut

            // Maak semaphores voor elke provider
            foreach (string provider in _requestsPerMinute.Keys)
            {
                _providerSemaphores[provider] = new SemaphoreSlim(1, 1);
                _lastRequestTimes[provider] = DateTime.MinValue;
            }
        }

        public async Task<bool> AcquireAsync(string provider, CancellationToken cancellationToken = default)
        {
            if (!_providerSemaphores.TryGetValue(provider, out SemaphoreSlim? semaphore))
            {
                return false;
            }

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                DateTime lastRequestTime = _lastRequestTimes[provider];
                int requestsPerMin = _requestsPerMinute[provider];

                TimeSpan timeSinceLastRequest = DateTime.Now - lastRequestTime;
                TimeSpan requestInterval = TimeSpan.FromMinutes(1) / requestsPerMin;

                if (timeSinceLastRequest < requestInterval)
                {
                    TimeSpan delay = requestInterval - timeSinceLastRequest;
                    await Task.Delay(delay, cancellationToken);
                }

                _lastRequestTimes[provider] = DateTime.Now;
                return true;
            }
            finally
            {
                _ = semaphore.Release();
            }
        }

        public void UpdateRateLimit(string provider, int requestsPerMinute)
        {
            _requestsPerMinute[provider] = requestsPerMinute;
        }
    }
}