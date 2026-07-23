using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize(Policy = "ManagerOrAbove")]
public class ReportsController : BaseController
{
    public ReportsController(AppDbContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var orgId = CurrentOrganizationId;
        var now = DateTime.UtcNow;

        var team = await Context.Teams
            .Include(t => t.Memberships).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.OrganizationId == orgId);
        if (team == null) return View(new TeamReportViewModel());

        var memberIds = team.Memberships.Select(m => m.UserId).ToList();
        var allOrgUsers = await Context.OrganizationMemberships
            .Where(m => m.OrganizationId == orgId)
            .Include(m => m.User)
            .ToListAsync();

        var employeeIds = allOrgUsers.Select(m => m.UserId).ToList();
        var tasks = await Context.Tasks
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

        foreach (var membership in allOrgUsers)
        {
            var user = membership.User;
            var userTasks = tasks.Where(t => t.Assignees.Any(a => a.UserId == user.Id)).ToList();

            var total = userTasks.Count;
            var completed = userTasks.Where(t => t.Status == TaskItemStatus.Done).ToList();
            var completedCount = completed.Count;

            var onTime = completed.Count(t => !t.DueDate.HasValue || t.DueDate >= (t.UpdatedAt ?? t.CreatedAt));
            var overdue = userTasks.Count(t => t.DueDate < now && t.Status != TaskItemStatus.Done);

            double? avgCycleHours = null;
            if (completed.Count > 0)
            {
                var cycles = completed
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

        var vm = new TeamReportViewModel
        {
            TeamSummary = teamSummary,
            Employees = employees.OrderByDescending(e => e.TotalTasks).ToList(),
            ChartLabels = chartLabels,
            ChartCompletionRates = chartCompletionRates,
            ChartOnTimeRates = chartOnTimeRates
        };

        return View(vm);
    }

    public async Task<IActionResult> EmployeeDetail(int id)
    {
        var orgId = CurrentOrganizationId;

        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var isInOrg = await Context.OrganizationMemberships
            .AnyAsync(m => m.OrganizationId == orgId && m.UserId == id);
        if (!isInOrg) return NotFound();

        var tasks = await Context.Tasks
            .Include(t => t.Category)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Where(t => t.OrganizationId == orgId && t.Assignees.Any(a => a.UserId == id))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        ViewBag.EmployeeName = user.Name;
        ViewBag.AvatarUrl = user.AvatarUrl;

        return View(tasks);
    }
}
