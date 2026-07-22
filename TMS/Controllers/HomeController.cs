using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        var totalTasksTask = _context.Tasks.CountAsync();
        var toDoTask = _context.Tasks.CountAsync(t => t.Status == Models.TaskItemStatus.ToDo);
        var inProgressTask = _context.Tasks.CountAsync(t => t.Status == Models.TaskItemStatus.InProgress);
        var doneTask = _context.Tasks.CountAsync(t => t.Status == Models.TaskItemStatus.Done);
        var urgentTask = _context.Tasks.CountAsync(t => t.Priority == Models.TaskPriority.Urgent && t.Status != Models.TaskItemStatus.Done);
        var overdueTask = _context.Tasks.CountAsync(t => t.DueDate < now && t.Status != Models.TaskItemStatus.Done);
        var usersTask = _context.Users.CountAsync();
        var categoriesTask = _context.Categories.CountAsync();
        var recentTask = _context.Tasks.Include(t => t.Category).Include(t => t.User).OrderByDescending(t => t.CreatedAt).Take(5).ToListAsync();
        var upcomingTask = _context.Tasks.Where(t => t.DueDate >= now && t.Status != Models.TaskItemStatus.Done).OrderBy(t => t.DueDate).Take(5).ToListAsync();
        var categorySummariesTask = _context.Categories.Select(c => new CategorySummary
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
