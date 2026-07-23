using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize]
public class HomeController : BaseController
{
    public HomeController(AppDbContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var uid = CurrentUserId;
        var orgId = CurrentOrganizationId;
        var now = DateTime.UtcNow;

        var orgQuery = Context.Tasks.Where(t => t.OrganizationId == orgId);
        var userQuery = orgQuery.Where(t => t.UserId == uid);

        var totalTasksTask = userQuery.CountAsync();
        var toDoTask = userQuery.CountAsync(t => t.Status == Models.TaskItemStatus.ToDo);
        var inProgressTask = userQuery.CountAsync(t => t.Status == Models.TaskItemStatus.InProgress);
        var doneTask = userQuery.CountAsync(t => t.Status == Models.TaskItemStatus.Done);
        var urgentTask = userQuery.CountAsync(t => t.Priority == Models.TaskPriority.Urgent && t.Status != Models.TaskItemStatus.Done);
        var overdueTask = userQuery.CountAsync(t => t.DueDate < now && t.Status != Models.TaskItemStatus.Done);
        var usersTask = Context.OrganizationMemberships.CountAsync(m => m.OrganizationId == orgId);
        var categoriesTask = Context.Categories.Where(c => c.OrganizationId == orgId).CountAsync();
        var recentTask = userQuery.Include(t => t.Category).Include(t => t.User).Include(t => t.Assignees).ThenInclude(a => a.User).OrderByDescending(t => t.CreatedAt).Take(5).ToListAsync();
        var upcomingTask = userQuery.Where(t => t.DueDate >= now && t.Status != Models.TaskItemStatus.Done).Include(t => t.Assignees).ThenInclude(a => a.User).OrderBy(t => t.DueDate).Take(5).ToListAsync();
        var categorySummariesTask = Context.Categories.Where(c => c.OrganizationId == orgId).Select(c => new CategorySummary
        {
            CategoryName = c.Name,
            Color = c.Color,
            TaskCount = c.Tasks.Count
        }).ToListAsync();

        await Task.WhenAll(totalTasksTask, toDoTask, inProgressTask, doneTask, urgentTask, overdueTask, usersTask, categoriesTask, recentTask, upcomingTask, categorySummariesTask);

        var vm = new DashboardViewModel
        {
            TotalTasks = await totalTasksTask,
            TasksToDo = await toDoTask,
            TasksInProgress = await inProgressTask,
            TasksDone = await doneTask,
            UrgentTasks = await urgentTask,
            OverdueTasks = await overdueTask,
            TotalUsers = await usersTask,
            TotalCategories = await categoriesTask,
            RecentTasks = await recentTask,
            UpcomingTasks = await upcomingTask,
            CategorySummaries = await categorySummariesTask
        };

        return View(vm);
    }
}
