using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AIHub.Models;
using AIHub.Configuration;
using System.Windows;

namespace AIHub.Services
{
    public interface IAuthService
    {
        AuthSession? CurrentSession { get; }
        UserProfile? CurrentUser { get; }
        bool IsSessionValid { get; }
        string GetAccessToken();
        Task<bool> LoginAsync(string email, string password, CancellationToken ct = default);
        Task<bool> RegisterAsync(string email, string password, string displayName, CancellationToken ct = default);
        Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);
        Task<bool> UpdateDisplayNameAsync(string newName, CancellationToken ct = default);
        Task<bool> SendPasswordResetAsync(string email, CancellationToken ct = default);
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;
        private readonly SupabaseConfig _config;
        private readonly string _supabaseUrl;
        private readonly string _supabaseAnonKey;
        private DateTime _sessionExpiry = DateTime.MinValue;

        public AuthSession? CurrentSession { get; private set; }
        public UserProfile? CurrentUser { get; private set; }
        public bool IsSessionValid => CurrentSession != null && DateTime.UtcNow < _sessionExpiry;

        public AuthService(HttpClient httpClient, IOptions<SupabaseConfig> config, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Value;

            _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? _config.Url;
            _supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? _config.AnonKey;

            if (!string.IsNullOrEmpty(_supabaseUrl))
            {
                _httpClient.BaseAddress = new Uri(_supabaseUrl);
            }

            ConfigureAnonymousHeaders();
        }

        public async Task<bool> LoginAsync(string email, string password, CancellationToken ct = default)
        {
            try
            {
                ConfigureAnonymousHeaders();

                var payload = new { email, password };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/auth/v1/token?grant_type=password", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    CurrentSession = JsonConvert.DeserializeObject<AuthSession>(json);
                    
                    if (CurrentSession?.user != null)
                    {
                        // Prefer absolute expiry when available, otherwise fallback to relative
                        if (CurrentSession.expires_at > 0)
                        {
                            _sessionExpiry = DateTimeOffset.FromUnixTimeSeconds(CurrentSession.expires_at).UtcDateTime;
                        }
                        else
                        {
                            _sessionExpiry = DateTime.UtcNow.AddSeconds(CurrentSession.expires_in > 0 ? CurrentSession.expires_in : 3600);
                        }
                        ApplyAuthHeader();
                        CurrentUser = new UserProfile 
                        { 
                            Id = CurrentSession.user.id, 
                            Email = CurrentSession.user.email,
                            DisplayName = email.Split('@')[0], 
                            Role = "Admin"
                        };
                        _logger.LogInformation("User {Email} logged in successfully.", email);
                        return true;
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Login failed for {Email}. Status: {StatusCode}. Body: {ErrorBody}", email, response.StatusCode, errorBody);
                    MessageBox.Show("Unable to log in. Please check your credentials.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoginAsync failed for {Email}", email);
                MessageBox.Show("Unable to log in due to a network or server error.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string email, string password, string displayName, CancellationToken ct = default)
        {
            try
            {
                ConfigureAnonymousHeaders();

                var payload = new { email, password, data = new { display_name = displayName } };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/auth/v1/signup", content, ct);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User {Email} registered successfully.", email);
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Registration failed for {Email}. Status: {StatusCode}. Body: {ErrorBody}", email, response.StatusCode, errorBody);
                    MessageBox.Show("Unable to register user.", "Registration Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegisterAsync failed for {Email}", email);
                MessageBox.Show("Unable to register due to a network or server error.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(string email, CancellationToken ct = default)
        {
            try
            {
                ConfigureAnonymousHeaders();

                var payload = new { email };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/auth/v1/recover", content, ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password reset email requested for {Email}.", email);
                    MessageBox.Show("If an account exists for this email, a password reset link has been sent.", "Password Reset", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }

                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Password reset failed for {Email}. Status: {StatusCode}. Body: {ErrorBody}", email, response.StatusCode, errorBody);
                MessageBox.Show("Unable to start password reset. Please check the email address.", "Password Reset Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendPasswordResetAsync failed for {Email}", email);
                MessageBox.Show("Unable to start password reset due to a network or server error.", "Password Reset Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
        {
            // In-memory only: if session is already set and still valid, return true
            return await Task.FromResult(IsSessionValid);
        }

        public Task<bool> UpdateDisplayNameAsync(string newName, CancellationToken ct = default)
        {
            if (CurrentUser == null || CurrentSession == null) return Task.FromResult(false);
            try
            {
                CurrentUser.DisplayName = newName;
                _logger.LogInformation("Display name updated to {DisplayName}.", newName);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateDisplayName failed.");
                return Task.FromResult(false);
            }
        }

        public void Logout()
        {
            if (CurrentSession != null)
            {
                _logger.LogInformation("User {Email} logged out.", CurrentSession.user?.email);
            }
            CurrentSession = null;
            CurrentUser = null;
            _sessionExpiry = DateTime.MinValue;
            ConfigureAnonymousHeaders();
        }

        public string GetAccessToken()
        {
            return CurrentSession?.access_token ?? string.Empty;
        }

        private void ApplyAuthHeader()
        {
            ConfigureAnonymousHeaders();

            if (!string.IsNullOrEmpty(CurrentSession?.access_token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {CurrentSession.access_token}");
            }
        }

        private void ConfigureAnonymousHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();

            if (!string.IsNullOrWhiteSpace(_supabaseAnonKey))
            {
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseAnonKey);
            }
        }
    }
}
