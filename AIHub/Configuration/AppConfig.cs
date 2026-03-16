namespace AIHub.Configuration
{
    public class SupabaseConfig
    {
        public string Url { get; set; } = string.Empty;
        public string AnonKey { get; set; } = string.Empty;
    }

    public class AIConfig
    {
        public string OpenAIKey { get; set; } = string.Empty;
        public string AnthropicKey { get; set; } = string.Empty;
        public string GeminiKey { get; set; } = string.Empty;
    }
}
