using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMS.Data;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(string? search, TaskItemStatus? status, int? categoryId, string? sort, int page = 1, int pageSize = 10)
    {
        var query = _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.User)
            .Where(t => t.UserId == CurrentUserId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        var total = await query.CountAsync();

        query = sort switch
        {
            "dueDate" => query.OrderBy(t => t.DueDate),
            "dueDateDesc" => query.OrderByDescending(t => t.DueDate),
            "priority" => query.OrderByDescending(t => t.Priority),
            "created" => query.OrderByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var tasks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.StatusFilter = status;
        ViewBag.CategoryFilter = categoryId;
        ViewBag.Sort = sort;

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.UserId == CurrentUserId).AsNoTracking().ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Statuses = new SelectList(Enum.GetValues<TaskItemStatus>().Cast<TaskItemStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }), "Value", "Text", status.HasValue ? (int)status.Value : null);

        return View(tasks);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var task = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null) return NotFound();

        return View(task);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new TaskViewModel
        {
            Categories = await _context.Categories.Where(c => c.UserId == CurrentUserId).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var task = new TaskItem
            {
                Title = vm.Title,
                Description = vm.Description,
                Status = vm.Status,
                Priority = vm.Priority,
                DueDate = vm.DueDate,
                CategoryId = vm.CategoryId,
                UserId = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(task);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Task created successfully!";
            return RedirectToAction(nameof(Index));
        }

        vm.Categories = await _context.Categories.Where(c => c.UserId == CurrentUserId).ToListAsync();
        return View(vm);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null) return NotFound();

        var vm = new TaskViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CategoryId = task.CategoryId,
            Categories = await _context.Categories.Where(c => c.UserId == CurrentUserId).ToListAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskViewModel vm)
    {
        if (id != vm.Id) return NotFound();

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null) return NotFound();

        if (ModelState.IsValid)
        {
            task.Title = vm.Title;
            task.Description = vm.Description;
            task.Status = vm.Status;
            task.Priority = vm.Priority;
            task.DueDate = vm.DueDate;
            task.CategoryId = vm.CategoryId;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Task updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        vm.Categories = await _context.Categories.Where(c => c.UserId == CurrentUserId).ToListAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, TaskItemStatus status)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null) return NotFound();

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Task status updated to {status}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var task = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task == null) return NotFound();

        return View(task);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Task deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }
}
