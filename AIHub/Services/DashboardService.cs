using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIHub.Models;
using AIHub.Repositories;

namespace AIHub.Services
{
    public class DashboardStats
    {
        public int CompletedTasksToday { get; set; }
        public int ActiveProjectsCount { get; set; }
        public int UpcomingMeetingsCount { get; set; }
        public int AITokensToday { get; set; }
        public string AITokensLabel { get; set; } = "0";
        public string SystemStatus { get; set; } = "Stable";
    }

    public interface IDashboardService
    {
        Task<DashboardStats> GetStatsAsync(CancellationToken ct = default);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ISupabaseRepository _repo;
        private readonly ILoggingService _logger;

        public DashboardService(ISupabaseRepository repo, ILoggingService logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<DashboardStats> GetStatsAsync(CancellationToken ct = default)
        {
            var stats = new DashboardStats();
            try
            {
                // Run all queries in parallel for performance
                var tasksTask     = _repo.GetTasksAsync(ct: ct);
                var projectsTask  = _repo.GetProjectsAsync(ct: ct);
                var meetingsTask  = _repo.GetMeetingsAsync(ct);
                var usageTask     = _repo.GetAIUsageAsync(ct);

                await Task.WhenAll(tasksTask, projectsTask, meetingsTask, usageTask);

                var today = DateTime.UtcNow.Date;

                // Completed tasks today
                stats.CompletedTasksToday = tasksTask.Result
                    .Count(t => t.IsCompleted);

                // Active projects
                stats.ActiveProjectsCount = projectsTask.Result
                    .Count(p => p.Status == "Active" || p.Status == "In Progress");

                // Upcoming meetings (we treat total count as upcoming)
                stats.UpcomingMeetingsCount = meetingsTask.Result.Count;

                // AI tokens used today
                var todayUsage = usageTask.Result
                    .Where(u => u.CreatedAt.Date == today)
                    .Sum(u => u.TokensUsed);
                stats.AITokensToday = todayUsage;
                stats.AITokensLabel = todayUsage >= 1000 ? $"{todayUsage / 1000.0:F1}k" : todayUsage.ToString();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "DashboardService.GetStatsAsync");
            }
            return stats;
        }
    }
}
