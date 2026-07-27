using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Provides implementations for generating organizational reports and employee efficiency metrics.</summary>
public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ReportService"/> class.</summary>
    /// <param name="context">The database context.</param>
    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Generates a team report with efficiency metrics for the organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>A view model containing team summary, employee efficiency data, and chart data.</returns>
    public async Task<TeamReportViewModel> GetTeamReportAsync(int orgId)
    {
        DateTime now = DateTime.UtcNow;

        Team? team = await _context.Teams
            .Include(t => t.Memberships).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.OrganizationId == orgId);
        if (team == null) return new TeamReportViewModel();

        List<TaskItem> tasks = await _context.Tasks
            .Include(t => t.Assignees)
            .Where(t => t.OrganizationId == orgId)
            .ToListAsync();

        var teamSummary = new TeamSummary
        {
            TeamName = team.Name,
            TotalMembers = team.Memberships.Count,
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.Status == TaskItemStatus.Done),
            OverdueTasks = tasks.Count(t => t.DueDate < now && t.Status != TaskItemStatus.Done),
            AvgCompletionRate = 0
        };

        var employees = new List<EmployeeEfficiency>();
        var chartLabels = new List<string>();
        var chartCompletionRates = new List<double>();
        var chartOnTimeRates = new List<double>();

        foreach (TeamMembership membership in team.Memberships)
        {
            User user = membership.User;
            var userTasks = tasks.Where(t => t.Assignees.Any(a => a.UserId == user.Id)).ToList();

            int total = userTasks.Count;
            var completed = userTasks.Where(t => t.Status == TaskItemStatus.Done).ToList();
            int completedCount = completed.Count;

            int onTime = completed.Count(t => !t.DueDate.HasValue || t.DueDate >= (t.UpdatedAt ?? t.CreatedAt));
            int overdue = userTasks.Count(t => t.DueDate < now && t.Status != TaskItemStatus.Done);

            double? avgCycleHours = null;
            if (completed.Count > 0)
            {
                IEnumerable<TimeSpan> cycles = completed
                    .Select(t => (t.UpdatedAt ?? t.CreatedAt) - t.CreatedAt)
                    .Where(d => d.TotalHours > 0);
                if (cycles.Any())
                    avgCycleHours = cycles.Average(d => d.TotalHours);
            }

            var workloads = userTasks
                .Where(t => t.Status != TaskItemStatus.Done)
                .GroupBy(t => t.Priority)
                .ToDictionary(g => g.Key, g => g.Count());

            employees.Add(new EmployeeEfficiency
            {
                UserId = user.Id,
                UserName = user.Name,
                AvatarUrl = user.AvatarUrl ?? "",
                TotalTasks = total,
                CompletedTasks = completedCount,
                CompletionRate = total > 0 ? Math.Round((double)completedCount / total * 100, 1) : 0,
                OnTimeTasks = onTime,
                OnTimeRate = completedCount > 0 ? Math.Round((double)onTime / completedCount * 100, 1) : 0,
                AvgCycleHours = avgCycleHours.HasValue ? Math.Round(avgCycleHours.Value, 1) : 0,
                OverdueCount = overdue,
                UrgentCount = workloads.GetValueOrDefault(TaskPriority.Urgent, 0),
                HighCount = workloads.GetValueOrDefault(TaskPriority.High, 0),
                MediumCount = workloads.GetValueOrDefault(TaskPriority.Medium, 0),
                LowCount = workloads.GetValueOrDefault(TaskPriority.Low, 0)
            });

            chartLabels.Add(user.Name.Split(' ')[0]);
            chartCompletionRates.Add(total > 0 ? Math.Round((double)completedCount / total * 100, 1) : 0);
            chartOnTimeRates.Add(completedCount > 0 ? Math.Round((double)onTime / completedCount * 100, 1) : 0);
        }

        teamSummary.AvgCompletionRate = employees.Any()
            ? Math.Round(employees.Average(e => e.CompletionRate), 1)
            : 0;

        return new TeamReportViewModel
        {
            TeamSummary = teamSummary,
            Employees = employees.OrderByDescending(e => e.TotalTasks).ToList(),
            ChartLabels = chartLabels,
            ChartCompletionRates = chartCompletionRates,
            ChartOnTimeRates = chartOnTimeRates
        };
    }

    /// <summary>Retrieves detailed task information for a specific employee within an organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="employeeId">The employee user identifier.</param>
    /// <returns>A tuple containing the employee's tasks, name, and avatar URL.</returns>
    public async Task<(List<TaskItem> Tasks, string EmployeeName, string AvatarUrl)> GetEmployeeDetailAsync(int orgId, int employeeId)
    {
        User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == employeeId);
        if (user == null) return (new List<TaskItem>(), "", "");

        bool isInOrg = await _context.OrganizationMemberships
            .AnyAsync(m => m.OrganizationId == orgId && m.UserId == employeeId);
        if (!isInOrg) return (new List<TaskItem>(), "", "");

        List<TaskItem> tasks = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Where(t => t.OrganizationId == orgId && t.Assignees.Any(a => a.UserId == employeeId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return (tasks, user.Name, user.AvatarUrl ?? "");
    }
}
