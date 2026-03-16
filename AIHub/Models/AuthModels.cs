using System;
using Newtonsoft.Json;

namespace AIHub.Models
{
    public class AuthSession
    {
        public string access_token { get; set; } = string.Empty;
        public string refresh_token { get; set; } = string.Empty;
        public int expires_in { get; set; }
        // Unix timestamp (seconds) when the token expires, as returned by Supabase
        public long expires_at { get; set; }
        public SupabaseAuthUser? user { get; set; }
    }

    public class SupabaseAuthUser
    {
        public string id { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
    }

    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = "Viewer";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Initials for avatar fallback (e.g. "AR" for Alex Rivera)</summary>
        public string Initials => string.IsNullOrWhiteSpace(DisplayName) ? "??"
            : string.Concat(DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0].ToString().ToUpper()));
    }

    public class ProjectMember
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("project_id")] public string ProjectId { get; set; } = string.Empty;
        [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonProperty("role")] public string Role { get; set; } = "Member";
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
