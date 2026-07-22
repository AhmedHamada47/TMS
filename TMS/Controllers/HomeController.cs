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

        var vm = new DashboardViewModel
        {
            TotalTasks = await _context.Tasks.CountAsync(),
            TasksToDo = await _context.Tasks.CountAsync(t => t.Status == Models.TaskItemStatus.ToDo),
            TasksInProgress = await _context.Tasks.CountAsync(t => t.Status == Models.TaskItemStatus.InProgress),
            TasksDone = await _context.Tasks.CountAsync(t => t.Status == Models.TaskItemStatus.Done),
            UrgentTasks = await _context.Tasks.CountAsync(t => t.Priority == Models.TaskPriority.Urgent && t.Status != Models.TaskItemStatus.Done),
            OverdueTasks = await _context.Tasks.CountAsync(t => t.DueDate < now && t.Status != Models.TaskItemStatus.Done),
            TotalUsers = await _context.Users.CountAsync(),
            TotalCategories = await _context.Categories.CountAsync(),
            RecentTasks = await _context.Tasks.Include(t => t.Category).Include(t => t.User).OrderByDescending(t => t.CreatedAt).Take(5).ToListAsync(),
            UpcomingTasks = await _context.Tasks.Where(t => t.DueDate >= now && t.Status != Models.TaskItemStatus.Done).OrderBy(t => t.DueDate).Take(5).ToListAsync(),
            CategorySummaries = await _context.Categories.Select(c => new CategorySummary
            {
                CategoryName = c.Name,
                Color = c.Color,
                TaskCount = c.Tasks.Count
            }).ToListAsync()
        };

        return View(vm);
    }
}
