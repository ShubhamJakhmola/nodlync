using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AIHub.Repositories;
using Newtonsoft.Json;

namespace AIHub.Services
{
    public interface IBackupService
    {
        Task<bool> ExportDataAsync(CancellationToken ct = default);
    }

    public class BackupService : IBackupService
    {
        private readonly ISupabaseRepository _repo;
        private readonly ILoggingService _logger;
        private readonly INotificationService _notifier;

        public BackupService(ISupabaseRepository repo, ILoggingService logger, INotificationService notifier)
        {
            _repo = repo;
            _logger = logger;
            _notifier = notifier;
        }

        public async Task<bool> ExportDataAsync(CancellationToken ct = default)
        {
            try
            {
                var exportData = new
                {
                    Projects = await _repo.GetProjectsAsync(ct: ct),
                    Tasks = await _repo.GetTasksAsync(ct: ct),
                    Notes = await _repo.GetProjectNotesAsync(ct: ct),
                    Workflows = await _repo.GetWorkflowsAsync(ct),
                    ApiKeys = await _repo.GetApiKeysAsync(ct) // Note: Values are already encrypted
                };

                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var folder = Path.Combine(docs, "AIHubBackups");
                Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, $"AIHub_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                
                await File.WriteAllTextAsync(filePath, json, ct);

                // Update last backup date in settings
                var settings = await _repo.GetSettingsAsync(ct) ?? new Models.AppSettings();
                settings.LastBackupDate = DateTime.UtcNow;
                await _repo.SaveSettingsAsync(settings, ct);

                _notifier.ShowNotification("Backup Successful", $"Data exported to {filePath}");
                await _logger.LogAsync("Backup", "SUCCESS", $"Exported full backup to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _notifier.ShowNotification("Backup Failed", "Failed to export data.");
                await _logger.LogErrorAsync(ex, "BackupService Export");
                return false;
            }
        }
    }
}
