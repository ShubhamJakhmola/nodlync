using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using AIHub.Configuration;

namespace AIHub.Services
{
    public interface ISupabaseSchemaInitializer
    {
        Task EnsureTablesExistAsync(CancellationToken ct = default);
    }

    public class SupabaseSchemaInitializer : ISupabaseSchemaInitializer
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService _logger;
        private readonly SupabaseConfig _config;

        public SupabaseSchemaInitializer(HttpClient httpClient, ILoggingService logger, IOptions<SupabaseConfig> config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;

            var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? _config.Url;
            var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? _config.AnonKey;

            if (!string.IsNullOrEmpty(url))
            {
                _httpClient.BaseAddress = new Uri(url);
            }

            if (!string.IsNullOrEmpty(anonKey))
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", anonKey);
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
            }
        }

        public async Task EnsureTablesExistAsync(CancellationToken ct = default)
        {
            var tables = new List<string> 
            { 
                "projects",
                "task_items",
                "project_notes",
                "project_activities",
                "api_key_items",
                "workflow_items",
                "meeting_links",
                "app_logs",
                "project_reports",
                "external_tools",
                "app_settings",
                "ai_usage_records",
                "user_profiles"
            };

            foreach (var table in tables)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"/rest/v1/{table}?limit=1", ct);
                    if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                    {
                        await _logger.LogAsync("Schema Check", "WARN", $"Table {table} might not exist or is inaccessible. Code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    await _logger.LogErrorAsync(ex, $"Schema Init for {table}");
                }
            }
        }
    }
}
