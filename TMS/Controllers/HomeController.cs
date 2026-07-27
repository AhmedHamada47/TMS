using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TMS.Data;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize]
public class HomeController : BaseController
{
    private readonly IMemoryCache _cache;

    public HomeController(AppDbContext context, IMemoryCache cache) : base(context)
    {
        _cache = cache;
    }

    /// <summary>
    /// Displays the dashboard with task statistics, recent tasks, upcoming tasks, and category summaries.
    /// Results are cached for 45 seconds.
    /// </summary>
    /// <returns>The dashboard view with a DashboardViewModel.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index()
    {
        int uid = CurrentUserId;
        int orgId = CurrentOrganizationId;
        string cacheKey = $"Dashboard_{uid}_{orgId}";

        if (_cache.TryGetValue(cacheKey, out DashboardViewModel? cached))
            return View(cached!);

        DateTime now = DateTime.UtcNow;

        IQueryable<TaskItem> orgQuery = Context.Tasks.Where(t => t.OrganizationId == orgId);
        IQueryable<TaskItem> userQuery = orgQuery.Where(t => t.UserId == uid);

        Task<int> totalTasksTask = userQuery.CountAsync();
        Task<int> toDoTask = userQuery.CountAsync(t => t.Status == Models.TaskItemStatus.ToDo);
        Task<int> inProgressTask = userQuery.CountAsync(t => t.Status == Models.TaskItemStatus.InProgress);
        Task<int> doneTask = userQuery.CountAsync(t => t.Status == Models.TaskItemStatus.Done);
        Task<int> urgentTask = userQuery.CountAsync(t => t.Priority == Models.TaskPriority.Urgent && t.Status != Models.TaskItemStatus.Done);
        Task<int> overdueTask = userQuery.CountAsync(t => t.DueDate < now && t.Status != Models.TaskItemStatus.Done);
        Task<int> usersTask = Context.OrganizationMemberships.CountAsync(m => m.OrganizationId == orgId);
        Task<int> categoriesTask = Context.Categories.Where(c => c.OrganizationId == orgId).CountAsync();
        Task<List<TaskItem>> recentTask = userQuery.Include(t => t.Category).Include(t => t.User).Include(t => t.Assignees).ThenInclude(a => a.User).OrderByDescending(t => t.CreatedAt).Take(5).ToListAsync();
        Task<List<TaskItem>> upcomingTask = userQuery.Where(t => t.DueDate >= now && t.Status != Models.TaskItemStatus.Done).Include(t => t.Assignees).ThenInclude(a => a.User).OrderBy(t => t.DueDate).Take(5).ToListAsync();
        Task<List<CategorySummary>> categorySummariesTask = Context.Categories.Where(c => c.OrganizationId == orgId).Select(c => new CategorySummary
        {
            CategoryName = c.Name,
            Color = c.Color,
            TaskCount = c.Tasks.Count
        }).ToListAsync();

        await Task.WhenAll(totalTasksTask, toDoTask, inProgressTask, doneTask, urgentTask, overdueTask, usersTask, categoriesTask, recentTask, upcomingTask, categorySummariesTask);

        var vm = new DashboardViewModel
        {
            TotalTasks = totalTasksTask.Result,
            TasksToDo = toDoTask.Result,
            TasksInProgress = inProgressTask.Result,
            TasksDone = doneTask.Result,
            UrgentTasks = urgentTask.Result,
            OverdueTasks = overdueTask.Result,
            TotalUsers = usersTask.Result,
            TotalCategories = categoriesTask.Result,
            RecentTasks = recentTask.Result,
            UpcomingTasks = upcomingTask.Result,
            CategorySummaries = categorySummariesTask.Result
        };

        _cache.Set(cacheKey, vm, TimeSpan.FromSeconds(45));

        return View(vm);
    }
}
