using System;
using System.Threading.Tasks;
using AIHub.Models;
using AIHub.Repositories;
using Microsoft.Extensions.Logging;

namespace AIHub.Services
{
    public interface ILoggingService
    {
        Task LogAsync(string action, string status, string details, string user = "System");
        Task LogErrorAsync(Exception ex, string context);
    }

    public class LoggingService : ILoggingService
    {
        private readonly ISupabaseRepository _supabase;
        private readonly ILogger<LoggingService> _systemLogger;

        public LoggingService(ISupabaseRepository supabase, ILogger<LoggingService> systemLogger)
        {
            _supabase = supabase;
            _systemLogger = systemLogger;
        }

        public async Task LogAsync(string action, string status, string details, string user = "System")
        {
            _systemLogger.LogInformation("[{Status}] {Action}: {Details} by {User}", status, action, details, user);
            try {
                var log = new AppLog
                {
                    Action = action,
                    Status = status,
                    Details = details,
                    User = user,
                    Timestamp = DateTime.UtcNow
                };
                await _supabase.CreateLogAsync(log);
            } catch (Exception ex) {
                _systemLogger.LogWarning(ex, "Remote log write failed for action {Action}", action);
            }
        }

        public async Task LogErrorAsync(Exception ex, string context)
        {
            _systemLogger.LogError(ex, "Error in {Context}", context);
            try {
                var log = new AppLog
                {
                    Action = context,
                    Status = "ERROR",
                    Details = ex.Message,
                    User = "System",
                    Timestamp = DateTime.UtcNow
                };
                await _supabase.CreateLogAsync(log);
            } catch (Exception remoteLogError) {
                _systemLogger.LogWarning(remoteLogError, "Remote error log write failed for context {Context}", context);
            }
        }
    }
}
