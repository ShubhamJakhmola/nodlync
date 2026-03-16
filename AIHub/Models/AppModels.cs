using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace AIHub.Models
{
    public class Project
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Map to Supabase column user_id
        [JsonProperty("user_id")]
        public string OwnerUserId { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = "Active";

        [JsonIgnore]
        public string DisplayStatus => ToDisplayStatus(Status);

        public static string ToDisplayStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "Active";
            }

            var cleaned = status.Trim();

            if (cleaned.Contains(':'))
            {
                cleaned = cleaned[(cleaned.LastIndexOf(':') + 1)..].Trim();
            }

            cleaned = cleaned.Replace("_", " ").Replace("-", " ");

            return string.IsNullOrWhiteSpace(cleaned)
                ? "Active"
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
        }
    }

    public class TaskItem
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("project_id")] public string ProjectId { get; set; } = string.Empty;
        [JsonProperty("title")] public string Title { get; set; } = string.Empty;
        [JsonProperty("status")] public string Status { get; set; } = "Pending";
        [JsonProperty("priority")] public string Priority { get; set; } = "Normal";
        [JsonProperty("assigned_user")] public string AssignedUser { get; set; } = string.Empty;
        [JsonProperty("due_date")] public DateTime? DueDate { get; set; }
        [JsonProperty("notes")] public string Notes { get; set; } = string.Empty;
        [JsonProperty("is_completed")] public bool IsCompleted { get; set; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Milestone
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("project_id")] public string ProjectId { get; set; } = string.Empty;
        [JsonProperty("title")] public string Title { get; set; } = string.Empty;
        [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        [JsonProperty("due_date")] public DateTime? DueDate { get; set; }
        [JsonProperty("completed")] public bool Completed { get; set; }
        [JsonIgnore] public string DisplayStatus => Completed ? "Completed" : "In Progress";
    }

    public class ProjectNote
    {
        private string _content = string.Empty;

        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("project_id")] public string ProjectId { get; set; } = string.Empty;
        [JsonProperty("content")] public string Content
        {
            get => _content;
            set => _content = value ?? string.Empty;
        }
        [JsonProperty("note")] public string LegacyNote { set => Content = value; }
        [JsonProperty("body")] public string LegacyBody { set => Content = value; }
        [JsonProperty("details")] public string LegacyDetails { set => Content = value; }
        [JsonProperty("text")] public string LegacyText { set => Content = value; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProjectBlocker
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("project_id")] public string ProjectId { get; set; } = string.Empty;
        [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        [JsonProperty("resolved")] public bool Resolved { get; set; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore] public string DisplayStatus => Resolved ? "Resolved" : "Open";
    }

    public class ApiKeyItem
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("provider")] public string Provider { get; set; } = string.Empty;
        [JsonProperty("encrypted_value")] public string EncryptedValue { get; set; } = string.Empty;
        [JsonProperty("initialization_vector")] public string InitializationVector { get; set; } = string.Empty;
        [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        [JsonProperty("tags")] public string Tags { get; set; } = string.Empty;
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SavedApiRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Authorization { get; set; } = string.Empty;
    }

    public class WorkflowItem
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("title")] public string Title { get; set; } = string.Empty;
        [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        [JsonProperty("category")] public string Category { get; set; } = string.Empty;
        [JsonProperty("file_path")] public string FilePath { get; set; } = string.Empty;
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProjectReport
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonProperty("project_name")] public string ProjectName { get; set; } = string.Empty;
        [JsonProperty("completed_tasks")] public List<string> CompletedTasks { get; set; } = new();
        [JsonProperty("next_steps")] public List<string> NextSteps { get; set; } = new();
        [JsonProperty("blockers")] public List<string> Blockers { get; set; } = new();
        [JsonProperty("report_date")] public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    public class MeetingLink
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("title")] public string Title { get; set; } = string.Empty;
        [JsonProperty("platform")] public string Platform { get; set; } = string.Empty;
        [JsonProperty("meeting_url")] public string MeetingUrl { get; set; } = string.Empty;
        [JsonProperty("notes")] public string Notes { get; set; } = string.Empty;
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AppLog
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonProperty("timestamp")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        [JsonIgnore] public string User { get; set; } = string.Empty;
        [JsonProperty("action")] public string Action { get; set; } = string.Empty;
        [JsonProperty("status")] public string Status { get; set; } = string.Empty;
        [JsonProperty("details")] public string Details { get; set; } = string.Empty;
    }

    public class ExternalTool
    {
         [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
         [JsonProperty("name")] public string Name { get; set; } = string.Empty;
         [JsonProperty("api_endpoint")] public string ApiEndpoint { get; set; } = string.Empty;
         [JsonProperty("auth_type")] public string AuthType { get; set; } = string.Empty;
         [JsonProperty("headers")] public string Headers { get; set; } = string.Empty;
         [JsonProperty("description")] public string Description { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonProperty("default_ai_provider")] public string DefaultAIProvider { get; set; } = "OpenAI";
        [JsonProperty("theme")] public string Theme { get; set; } = "Dark";
        [JsonProperty("auto_update_enabled")] public bool AutoUpdateEnabled { get; set; } = true;
        [JsonProperty("notifications_enabled")] public bool NotificationsEnabled { get; set; } = true;
        [JsonProperty("default_project_view")] public string DefaultProjectView { get; set; } = "List";
        [JsonProperty("last_backup_date")] public DateTime? LastBackupDate { get; set; }
    }

    public class AIUsageRecord
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonProperty("provider")] public string Provider { get; set; } = string.Empty;
        [JsonProperty("tokens_used")] public int TokensUsed { get; set; } = 0;
        [JsonProperty("prompt_type")] public string PromptType { get; set; } = string.Empty;
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProjectActivity
    {
        [JsonProperty("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("project_id")] public string ProjectId { get; set; } = string.Empty;
        [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
        [JsonProperty("action")] public string Action { get; set; } = string.Empty;
        [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
