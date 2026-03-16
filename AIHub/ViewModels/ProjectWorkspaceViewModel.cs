using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIHub.Models;
using AIHub.Repositories;
using AIHub.Services;

namespace AIHub.ViewModels
{
    public partial class ProjectWorkspaceViewModel : ObservableObject
    {
        private readonly ISupabaseRepository _repo;
        private readonly ILoggingService _logger;

        [ObservableProperty] private Project? _currentProject;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _completedWork = string.Empty;
        [ObservableProperty] private string _nextSteps = string.Empty;
        [ObservableProperty] private string _dailyBlockers = string.Empty;
        [ObservableProperty] private string _dailyNotes = string.Empty;
        [ObservableProperty] private string _workspaceNotice = string.Empty;

        public ObservableCollection<TaskItem> Tasks { get; } = new();
        public ObservableCollection<ProjectNote> Notes { get; } = new();
        public ObservableCollection<Milestone> Milestones { get; } = new();
        public ObservableCollection<ProjectBlocker> Blockers { get; } = new();
        public ObservableCollection<ProjectMember> Members { get; } = new();
        public ObservableCollection<ProjectReport> Reports { get; } = new();
        public ObservableCollection<ProjectWorkspaceActivityItem> ActivityTimeline { get; } = new();
        public ObservableCollection<Milestone> MilestoneHighlights { get; } = new();
        public ObservableCollection<ProjectTeamAvatar> TeamAvatars { get; } = new();
        public ObservableCollection<ProjectWorkspaceTeamMember> TeamMembers { get; } = new();

        public Action? OnBackRequested { get; set; }

        public IRelayCommand BackCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand PostDailyLogCommand { get; }
        public IAsyncRelayCommand GenerateTodayReportCommand { get; }
        public IAsyncRelayCommand GenerateFullReportCommand { get; }
        public IRelayCommand ExportWorkspaceReportCommand { get; }

        public string ProjectName => CurrentProject?.Name ?? "Project Workspace";
        public string StatusText => CurrentProject?.DisplayStatus ?? "Active";
        public string DescriptionText => string.IsNullOrWhiteSpace(CurrentProject?.Description)
            ? "No description added yet."
            : CurrentProject.Description;
        public string StartDateText => CurrentProject == null ? "-" : CurrentProject.CreatedAt.ToString("MMM dd, yyyy");
        public string LastUpdatedText => GetLastUpdated().ToString("MMM dd, yyyy HH:mm");
        public string TotalHoursText => $"{Tasks.Sum(task => task.IsCompleted ? 4 : 1)} hrs";
        public string VelocityText => $"{Tasks.Count(task => task.IsCompleted && task.CreatedAt >= DateTime.UtcNow.AddDays(-7))} completed this week";
        public string ProgressText => Tasks.Count == 0
            ? "No tasks added yet"
            : $"{Tasks.Count(task => task.IsCompleted)} of {Tasks.Count} tasks complete";
        public double ProgressPercent => Tasks.Count == 0 ? 0 : (double)Tasks.Count(task => task.IsCompleted) / Tasks.Count * 100d;
        public int CompletedMilestonesCount => Milestones.Count(milestone => milestone.Completed);
        public int InProgressMilestonesCount => Milestones.Count(milestone => !milestone.Completed && (milestone.DueDate == null || milestone.DueDate >= DateTime.UtcNow));
        public int UpcomingMilestonesCount => Milestones.Count(milestone => !milestone.Completed && milestone.DueDate > DateTime.UtcNow.AddDays(7));
        public bool HasProject => CurrentProject != null;
        public string QuickReportHint => Reports.Count == 0
            ? "Generate a report snapshot for today or the full workspace."
            : $"Latest report: {Reports.First().ReportDate:MMM dd, yyyy HH:mm}";

        public ProjectWorkspaceViewModel(ISupabaseRepository repo, ILoggingService logger)
        {
            _repo = repo;
            _logger = logger;

            BackCommand = new RelayCommand(() => OnBackRequested?.Invoke());
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => CurrentProject != null);
            PostDailyLogCommand = new AsyncRelayCommand(PostDailyLogAsync, CanPostDailyLog);
            GenerateTodayReportCommand = new AsyncRelayCommand(GenerateTodayReportAsync, () => CurrentProject != null);
            GenerateFullReportCommand = new AsyncRelayCommand(GenerateFullReportAsync, () => CurrentProject != null);
            ExportWorkspaceReportCommand = new RelayCommand(ExportWorkspaceReport, () => CurrentProject != null);
        }

        public async Task LoadProjectAsync(Project project)
        {
            CurrentProject = project;
            await RefreshAsync();
        }

        partial void OnCurrentProjectChanged(Project? value)
        {
            RefreshCommand.NotifyCanExecuteChanged();
            PostDailyLogCommand.NotifyCanExecuteChanged();
            GenerateTodayReportCommand.NotifyCanExecuteChanged();
            GenerateFullReportCommand.NotifyCanExecuteChanged();
            ExportWorkspaceReportCommand.NotifyCanExecuteChanged();
            NotifySummaryProperties();
        }

        partial void OnCompletedWorkChanged(string value) => PostDailyLogCommand.NotifyCanExecuteChanged();
        partial void OnNextStepsChanged(string value) => PostDailyLogCommand.NotifyCanExecuteChanged();
        partial void OnDailyBlockersChanged(string value) => PostDailyLogCommand.NotifyCanExecuteChanged();
        partial void OnDailyNotesChanged(string value) => PostDailyLogCommand.NotifyCanExecuteChanged();

        private bool CanPostDailyLog()
        {
            return CurrentProject != null && new[] { CompletedWork, NextSteps, DailyBlockers, DailyNotes }.Any(text => !string.IsNullOrWhiteSpace(text));
        }

        private async Task RefreshAsync()
        {
            if (CurrentProject == null)
            {
                return;
            }

            IsLoading = true;
            WorkspaceNotice = string.Empty;
            try
            {
                var latestProjectTask = LoadWorkspaceDataAsync(() => _repo.GetProjectsAsync(true), "ProjectWorkspaceProjects");
                var tasksTask = LoadWorkspaceDataAsync(() => _repo.GetTasksAsync(CurrentProject.Id), "ProjectWorkspaceTasks");
                var notesTask = LoadWorkspaceDataAsync(() => _repo.GetProjectNotesAsync(CurrentProject.Id), "ProjectWorkspaceNotes");
                var milestonesTask = LoadWorkspaceDataAsync(() => _repo.GetMilestonesAsync(CurrentProject.Id), "ProjectWorkspaceMilestones");
                var blockersTask = LoadWorkspaceDataAsync(() => _repo.GetProjectBlockersAsync(CurrentProject.Id), "ProjectWorkspaceBlockers");
                var membersTask = LoadWorkspaceDataAsync(() => _repo.GetProjectMembersAsync(CurrentProject.Id), "ProjectWorkspaceMembers");
                var reportsTask = LoadWorkspaceDataAsync(() => _repo.GetReportsAsync(), "ProjectWorkspaceReports");
                var activityTask = LoadWorkspaceDataAsync(() => _repo.GetProjectActivitiesAsync(CurrentProject.Id), "ProjectWorkspaceActivity");

                await Task.WhenAll(latestProjectTask, tasksTask, notesTask, milestonesTask, blockersTask, membersTask, reportsTask, activityTask);

                var latestProject = latestProjectTask.Result.FirstOrDefault(project => project.Id == CurrentProject.Id);
                if (latestProject != null)
                {
                    CurrentProject = latestProject;
                }

                ReplaceCollection(Tasks, tasksTask.Result.OrderBy(task => task.DueDate ?? DateTime.MaxValue).ThenBy(task => task.CreatedAt));
                ReplaceCollection(Notes, notesTask.Result.OrderByDescending(note => note.CreatedAt));
                ReplaceCollection(Milestones, milestonesTask.Result.OrderBy(milestone => milestone.DueDate ?? DateTime.MaxValue));
                ReplaceCollection(Blockers, blockersTask.Result.OrderByDescending(blocker => blocker.CreatedAt));
                ReplaceCollection(Members, membersTask.Result.OrderBy(member => member.CreatedAt));
                ReplaceCollection(
                    Reports,
                    reportsTask.Result
                        .Where(report => string.Equals(report.ProjectName, CurrentProject.Name, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(report => report.ReportDate));

                var activities = BuildActivityTimeline(activityTask.Result);
                ReplaceCollection(ActivityTimeline, activities);
                ReplaceCollection(MilestoneHighlights, Milestones.Take(3));
                ReplaceCollection(TeamMembers, BuildTeamMembers());
                ReplaceCollection(TeamAvatars, TeamMembers.Take(5).Select(member => new ProjectTeamAvatar
                {
                    Label = $"{member.DisplayName} ({member.Role})",
                    Initials = member.Initials
                }));
                NotifySummaryProperties();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "ProjectWorkspaceRefresh");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateTodayReportAsync()
        {
            if (CurrentProject == null)
            {
                return;
            }

            try
            {
                var report = new ProjectReport
                {
                    UserId = CurrentProject.OwnerUserId,
                    ProjectName = CurrentProject.Name,
                    CompletedTasks = Tasks.Where(task => task.IsCompleted && task.CreatedAt >= DateTime.UtcNow.AddDays(-1)).Select(task => task.Title).ToList(),
                    NextSteps = Tasks.Where(task => !task.IsCompleted).Take(5).Select(task => task.Title).ToList(),
                    Blockers = Blockers.Where(blocker => !blocker.Resolved).Select(blocker => blocker.Description).ToList()
                };

                await _repo.CreateReportAsync(report);
                await TryAddActivityAsync("Today's Report", "Generated a daily project summary.");

                await RefreshAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "ProjectWorkspaceTodayReport");
            }
        }

        private async Task GenerateFullReportAsync()
        {
            if (CurrentProject == null)
            {
                return;
            }

            try
            {
                var report = new ProjectReport
                {
                    UserId = CurrentProject.OwnerUserId,
                    ProjectName = CurrentProject.Name,
                    CompletedTasks = Tasks.Where(task => task.IsCompleted).Select(task => task.Title).ToList(),
                    NextSteps = Tasks.Where(task => !task.IsCompleted).Select(task => task.Title).ToList(),
                    Blockers = Blockers.Where(blocker => !blocker.Resolved).Select(blocker => blocker.Description).ToList()
                };

                await _repo.CreateReportAsync(report);
                await TryAddActivityAsync("Full Project Report", "Generated a full project workspace report.");

                await RefreshAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "ProjectWorkspaceFullReport");
            }
        }

        private void ExportWorkspaceReport()
        {
            if (CurrentProject == null)
            {
                return;
            }

            try
            {
                Clipboard.SetText(BuildWorkspaceSummary());
            }
            catch
            {
                // Clipboard access is best-effort only.
            }
        }

        private async Task PostDailyLogAsync()
        {
            if (CurrentProject == null)
            {
                return;
            }

            try
            {
                var sections = new List<string>();

                if (!string.IsNullOrWhiteSpace(CompletedWork))
                {
                    sections.Add($"Completed Work\n{CompletedWork.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(NextSteps))
                {
                    sections.Add($"Next Steps\n{NextSteps.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(DailyBlockers))
                {
                    sections.Add($"Blockers\n{DailyBlockers.Trim()}");
                }

                if (!string.IsNullOrWhiteSpace(DailyNotes))
                {
                    sections.Add($"Notes\n{DailyNotes.Trim()}");
                }

                var note = new ProjectNote
                {
                    ProjectId = CurrentProject.Id,
                    Content = string.Join("\n\n", sections)
                };

                await _repo.CreateProjectNoteAsync(note);
                await TryAddActivityAsync("Daily Log", "Posted a daily work log.");

                CompletedWork = string.Empty;
                NextSteps = string.Empty;
                DailyBlockers = string.Empty;
                DailyNotes = string.Empty;

                await RefreshAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "ProjectWorkspacePostDailyLog");
            }
        }

        private IEnumerable<ProjectWorkspaceActivityItem> BuildActivityTimeline(IEnumerable<ProjectActivity> activities)
        {
            var list = activities
                .OrderByDescending(activity => activity.CreatedAt)
                .Select(activity => new ProjectWorkspaceActivityItem
                {
                    Title = string.IsNullOrWhiteSpace(activity.Action) ? "Project activity" : activity.Action,
                    Description = string.IsNullOrWhiteSpace(activity.Description) ? "Activity recorded." : activity.Description,
                    Timestamp = activity.CreatedAt
                })
                .ToList();

            if (!list.Any() && CurrentProject != null)
            {
                list.Add(new ProjectWorkspaceActivityItem
                {
                    Title = "Project created",
                    Description = $"{CurrentProject.Name} is ready for planning.",
                    Timestamp = CurrentProject.CreatedAt
                });
            }

            foreach (var note in Notes.Take(3))
            {
                list.Add(new ProjectWorkspaceActivityItem
                {
                    Title = "Note added",
                    Description = note.Content.Length > 80 ? note.Content[..80] + "..." : note.Content,
                    Timestamp = note.CreatedAt
                });
            }

            return list
                .OrderByDescending(item => item.Timestamp)
                .Take(12)
                .ToList();
        }

        private IEnumerable<ProjectWorkspaceTeamMember> BuildTeamMembers()
        {
            if (Members.Count == 0 && CurrentProject != null)
            {
                return new[]
                {
                    new ProjectWorkspaceTeamMember
                    {
                        DisplayName = "Project Owner",
                        Role = "Owner",
                        Initials = "OW"
                    }
                };
            }

            return Members.Select(member => new ProjectWorkspaceTeamMember
            {
                DisplayName = BuildDisplayName(member.UserId),
                Role = string.IsNullOrWhiteSpace(member.Role) ? "Contributor" : member.Role,
                Initials = BuildInitials(member.UserId)
            });
        }

        private DateTime GetLastUpdated()
        {
            var timestamps = new List<DateTime>();

            if (CurrentProject != null)
            {
                timestamps.Add(CurrentProject.CreatedAt);
            }

            timestamps.AddRange(Notes.Select(note => note.CreatedAt));
            timestamps.AddRange(Blockers.Select(blocker => blocker.CreatedAt));
            timestamps.AddRange(Reports.Select(report => report.ReportDate));
            timestamps.AddRange(ActivityTimeline.Select(item => item.Timestamp));

            return timestamps.Count == 0 ? DateTime.UtcNow : timestamps.Max();
        }

        private async Task<List<T>> LoadWorkspaceDataAsync<T>(Func<Task<List<T>>> loader, string context)
        {
            try
            {
                return await loader();
            }
            catch (Exception ex)
            {
                WorkspaceNotice = "Some workspace data could not be loaded from Supabase. Available sections are still shown.";
                await _logger.LogErrorAsync(ex, context);
                return new List<T>();
            }
        }

        private async Task TryAddActivityAsync(string action, string description)
        {
            if (CurrentProject == null)
            {
                return;
            }

            try
            {
                await _repo.CreateProjectActivityAsync(new ProjectActivity
                {
                    ProjectId = CurrentProject.Id,
                    UserId = CurrentProject.OwnerUserId,
                    Action = action,
                    Description = description
                });
            }
            catch (Exception ex)
            {
                WorkspaceNotice = "Workspace activity logging is unavailable right now, but the rest of the project data is still usable.";
                await _logger.LogErrorAsync(ex, "ProjectWorkspaceActivityWrite");
            }
        }

        private void NotifySummaryProperties()
        {
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(DescriptionText));
            OnPropertyChanged(nameof(StartDateText));
            OnPropertyChanged(nameof(LastUpdatedText));
            OnPropertyChanged(nameof(TotalHoursText));
            OnPropertyChanged(nameof(VelocityText));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(CompletedMilestonesCount));
            OnPropertyChanged(nameof(InProgressMilestonesCount));
            OnPropertyChanged(nameof(UpcomingMilestonesCount));
            OnPropertyChanged(nameof(HasProject));
            OnPropertyChanged(nameof(QuickReportHint));
        }

        private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static string BuildInitials(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "??";
            }

            var parts = value
                .Split(new[] { '.', '-', '_', '@' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            if (parts.Count == 0)
            {
                return "??";
            }

            return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
        }

        private static string BuildDisplayName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unknown Member";
            }

            var cleaned = value.Split('@')[0].Replace('.', ' ').Replace('-', ' ').Replace('_', ' ');
            var words = cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());

            var displayName = string.Join(" ", words);
            return string.IsNullOrWhiteSpace(displayName) ? value : displayName;
        }

        private string BuildWorkspaceSummary()
        {
            return
$@"Project: {ProjectName}
Status: {StatusText}
Progress: {ProgressText}
Start Date: {StartDateText}
Last Updated: {LastUpdatedText}
Total Hours: {TotalHoursText}
Velocity: {VelocityText}

Team
{string.Join(Environment.NewLine, TeamMembers.Select(member => $"- {member.DisplayName} ({member.Role})"))}

Milestones
{string.Join(Environment.NewLine, Milestones.Take(5).Select(milestone => $"- {milestone.Title} [{milestone.DisplayStatus}]"))}

Open Tasks
{string.Join(Environment.NewLine, Tasks.Where(task => !task.IsCompleted).Take(8).Select(task => $"- {task.Title}"))}

Open Blockers
{string.Join(Environment.NewLine, Blockers.Where(blocker => !blocker.Resolved).Take(8).Select(blocker => $"- {blocker.Description}"))}";
        }
    }

    public class ProjectWorkspaceActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string TimestampText => Timestamp.ToLocalTime().ToString("MMM dd, yyyy HH:mm");
    }

    public class ProjectTeamAvatar
    {
        public string Label { get; set; } = string.Empty;
        public string Initials { get; set; } = "??";
    }

    public class ProjectWorkspaceTeamMember
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = "Contributor";
        public string Initials { get; set; } = "??";
    }
}
