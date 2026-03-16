using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AIHub.Configuration;
using Microsoft.Extensions.Options;

namespace AIHub.Services
{
    public interface IHealthService
    {
        Task<string> CheckHealthAsync(CancellationToken ct = default);
    }

    public class HealthService : IHealthService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseConfig _supabaseConfig;
        private readonly AIConfig _aiConfig;

        public HealthService(HttpClient httpClient, IOptions<SupabaseConfig> supabaseConfig, IOptions<AIConfig> aiConfig)
        {
            _httpClient = httpClient;
            _supabaseConfig = supabaseConfig.Value;
            _aiConfig = aiConfig.Value;
        }

        public async Task<string> CheckHealthAsync(CancellationToken ct = default)
        {
            var warnings = "";

            // Check Supabase config
            var sbUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? _supabaseConfig.Url;
            if (string.IsNullOrEmpty(sbUrl))
                warnings += "Supabase URL is missing configured. ";

            // Check AI Keys loosely
            var aiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _aiConfig.OpenAIKey;
            if (string.IsNullOrEmpty(aiKey))
                warnings += "OpenAI key is missing! AI features may fail. ";

            // Check network to Supabase passively (can test PostgREST root)
            if (!string.IsNullOrEmpty(sbUrl))
            {
                try
                {
                    var response = await _httpClient.GetAsync(sbUrl, ct);
                    // Just reaching the server without crashing is a pass
                }
                catch
                {
                    warnings += $"Failed network connection to {sbUrl}. ";
                }
            }

            return warnings;
        }
    }
}
