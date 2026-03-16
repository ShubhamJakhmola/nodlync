using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using AIHub.Models;
using AIHub.Configuration;
using AIHub.Repositories;

namespace AIHub.Services
{
    public interface IAIService
    {
        Task<string> GenerateTextAsync(string prompt, string provider, string apiKeyContext, CancellationToken ct = default);
        Task<string> GenerateIdeasAsync(string topic, string provider, string apiKeyContext, CancellationToken ct = default);
        Task<string> GenerateWorkflowAsync(string requirements, string provider, string apiKeyContext, CancellationToken ct = default);
        Task<string> GenerateFlowDiagramAsync(string requirements, string provider, string apiKeyContext, CancellationToken ct = default);
    }

    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService _logger;
        private readonly AIConfig _config;
        private readonly ISupabaseRepository _repo;

        public AIService(HttpClient httpClient, ILoggingService logger, IOptions<AIConfig> config, ISupabaseRepository repo)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;
            _repo = repo;
        }

        private string GetApiKey(string provider, string fallbackContextKey)
        {
            if (provider == "OpenAI")
                return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _config.OpenAIKey ?? fallbackContextKey;
            if (provider == "Anthropic Claude")
                return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? _config.AnthropicKey ?? fallbackContextKey;
            if (provider == "Gemini")
                return Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _config.GeminiKey ?? fallbackContextKey;
                
            return fallbackContextKey; 
        }

        private async Task TrackUsageAsync(string provider, int estimatedTokens, string promptType)
        {
            try {
                await _repo.CreateAIUsageAsync(new AIUsageRecord {
                    Provider = provider,
                    TokensUsed = estimatedTokens,
                    PromptType = promptType
                });
            } catch { /* suppress */ }
        }

        public async Task<string> GenerateTextAsync(string prompt, string provider, string apiKeyContext, CancellationToken ct = default)
        {
            await _logger.LogAsync("AI Generate Text", "INFO", $"Provider: {provider}");
            var apiKey = GetApiKey(provider, apiKeyContext);
            
            try 
            {
                string resText = string.Empty;
                if (provider == "OpenAI")
                {
                    var reqBody = new { model = "gpt-4-turbo", messages = new[] { new { role = "user", content = prompt } } };
                    var content = new StringContent(JsonConvert.SerializeObject(reqBody), Encoding.UTF8, "application/json");
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions") { Content = content };
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                    var response = await _httpClient.SendAsync(request, ct);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync(ct);
                    dynamic result = JsonConvert.DeserializeObject(json)!;
                    resText = result.choices[0].message.content;
                }
                else if (provider == "Anthropic Claude")
                {
                    var reqBody = new { model = "claude-3-opus-20240229", max_tokens = 1024, messages = new[] { new { role = "user", content = prompt } } };
                    var content = new StringContent(JsonConvert.SerializeObject(reqBody), Encoding.UTF8, "application/json");
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages") { Content = content };
                    request.Headers.Add("x-api-key", apiKey);
                    request.Headers.Add("anthropic-version", "2023-06-01");
                    var response = await _httpClient.SendAsync(request, ct);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync(ct);
                    dynamic result = JsonConvert.DeserializeObject(json)!;
                    resText = result.content[0].text;
                }
                else 
                {
                    resText = $"[Simulated {provider}] Response: {prompt}";
                }

                await TrackUsageAsync(provider, prompt.Length + resText.Length, "GenerateText");
                return resText;
            }
            catch(Exception ex)
            {
                await _logger.LogErrorAsync(ex, $"AI Gen ({provider})");
                return $"Error calling {provider}: {ex.Message}";
            }
        }

        public async Task<string> GenerateIdeasAsync(string topic, string provider, string apiKeyContext, CancellationToken ct = default)
            => await GenerateTextAsync($"Generate 3 powerful project or automation ideas about: {topic}", provider, apiKeyContext, ct);

        public async Task<string> GenerateWorkflowAsync(string requirements, string provider, string apiKeyContext, CancellationToken ct = default)
            => await GenerateTextAsync($"Create a step-by-step automation workflow for: {requirements}", provider, apiKeyContext, ct);

        public async Task<string> GenerateFlowDiagramAsync(string requirements, string provider, string apiKeyContext, CancellationToken ct = default)
            => await GenerateTextAsync($"Generate a Mermaid.js flowchart for: {requirements}", provider, apiKeyContext, ct);
    }
}
