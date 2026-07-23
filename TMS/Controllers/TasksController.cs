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
public class TasksController : BaseController
{
    public TasksController(AppDbContext context) : base(context) { }

    public async Task<IActionResult> Board()
    {
        var orgId = CurrentOrganizationId;

        var columns = await Context.BoardColumns
            .Where(bc => bc.Board.Project.OrganizationId == orgId)
            .Include(bc => bc.Tasks.Where(t => IsManagerOrAbove || t.Assignees.Any(a => a.UserId == CurrentUserId)))
                .ThenInclude(t => t.Assignees).ThenInclude(a => a.User)
            .Include(bc => bc.Tasks).ThenInclude(t => t.Category)
            .OrderBy(bc => bc.Order)
            .ToListAsync();

        var board = await Context.Boards
            .Include(b => b.Project)
            .FirstOrDefaultAsync(b => b.Project.OrganizationId == orgId);

        var vm = new BoardViewModel
        {
            BoardName = board?.Name ?? "Board",
            ProjectName = board?.Project?.Name ?? "",
            Columns = columns.Select(c => new BoardColumnViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Tasks = c.Tasks.OrderBy(t => t.BoardOrder).ToList()
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBoardPosition(int taskId, int columnId, int order)
    {
        var task = await Context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == CurrentOrganizationId);

        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return Forbid();

        var column = await Context.BoardColumns
            .FirstOrDefaultAsync(bc => bc.Id == columnId && bc.Board.Project.OrganizationId == CurrentOrganizationId);

        if (column == null) return NotFound();

        task.BoardColumnId = columnId;
        task.BoardOrder = order;
        task.UpdatedAt = DateTime.UtcNow;

        await Context.SaveChangesAsync();

        return Ok();
    }

    private IQueryable<TaskItem> OrgScopedTasks() =>
        Context.Tasks.Where(t => t.OrganizationId == CurrentOrganizationId);

    private async Task<List<User>> GetTeamMembersAsync()
    {
        var teamMemberIds = await Context.TeamMemberships
            .Where(tm => tm.Team.OrganizationId == CurrentOrganizationId && tm.UserId == CurrentUserId)
            .Select(tm => tm.TeamId)
            .FirstOrDefaultAsync();

        if (teamMemberIds == 0)
            return new List<User>();

        return await Context.TeamMemberships
            .Where(tm => tm.TeamId == teamMemberIds)
            .Select(tm => tm.User)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IActionResult> Index(string? search, TaskItemStatus? status, int? categoryId, string? sort, int page = 1, int pageSize = 10)
    {
        IQueryable<TaskItem> query;

        if (IsManagerOrAbove)
        {
            query = Context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Assignees).ThenInclude(a => a.User)
                .Where(t => t.OrganizationId == CurrentOrganizationId);
        }
        else
        {
            query = Context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Assignees).ThenInclude(a => a.User)
                .Where(t => t.OrganizationId == CurrentOrganizationId && t.Assignees.Any(a => a.UserId == CurrentUserId));
        }

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
        ViewBag.Categories = new SelectList(await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).AsNoTracking().ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Statuses = new SelectList(Enum.GetValues<TaskItemStatus>().Cast<TaskItemStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }), "Value", "Text", status.HasValue ? (int)status.Value : null);
        ViewBag.IsManager = IsManagerOrAbove;

        return View(tasks);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var task = await Context.Tasks
            .Include(t => t.Category)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Include(t => t.Comments).ThenInclude(c => c.User)
            .Include(t => t.Comments).ThenInclude(c => c.Replies).ThenInclude(r => r.User)
            .Include(t => t.ActivityLogs).ThenInclude(al => al.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == CurrentOrganizationId);

        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return NotFound();

        return View(task);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int taskId, string content, int? parentCommentId)
    {
        var task = await Context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == CurrentOrganizationId);

        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return NotFound();

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        var comment = new TaskComment
        {
            TaskItemId = taskId,
            UserId = CurrentUserId,
            Content = content.Trim(),
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        Context.TaskComments.Add(comment);
        await Context.SaveChangesAsync();

        await LogActivityAsync(taskId, "Comment", null, "Added a comment");

        var assigneeIds = task.Assignees.Where(a => a.UserId != CurrentUserId).Select(a => a.UserId).ToList();
        foreach (var uid in assigneeIds)
        {
            await CreateNotificationAsync(uid, $"{CurrentUserName} commented on \"{task.Title}\"", $"/Tasks/Details/{taskId}");
        }

        TempData["Success"] = "Comment added.";
        return RedirectToAction(nameof(Details), new { id = taskId });
    }

    public async Task<IActionResult> Create()
    {
        var vm = new TaskViewModel
        {
            Categories = await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).ToListAsync(),
            TeamMembers = IsManagerOrAbove ? await GetTeamMembersAsync() : new List<User>(),
            AssigneeId = IsManagerOrAbove ? null : CurrentUserId
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
                OrganizationId = CurrentOrganizationId,
                CreatedAt = DateTime.UtcNow
            };

            Context.Add(task);
            await Context.SaveChangesAsync();

            var assigneeId = IsManagerOrAbove ? (vm.AssigneeId ?? CurrentUserId) : CurrentUserId;

            Context.TaskAssignees.Add(new TaskAssignee
            {
                TaskItemId = task.Id,
                UserId = assigneeId,
                IsPrimary = true
            });
            await Context.SaveChangesAsync();

            await LogActivityAsync(task.Id, "Task", null, $"Created task \"{task.Title}\"");
            if (assigneeId != CurrentUserId)
            {
                var userName = await Context.Users.Where(u => u.Id == assigneeId).Select(u => u.Name).FirstOrDefaultAsync();
                await CreateNotificationAsync(assigneeId, $"{CurrentUserName} assigned you to \"{task.Title}\"", $"/Tasks/Details/{task.Id}");
            }

            TempData["Success"] = "Task created successfully!";
            return RedirectToAction(nameof(Index));
        }

        vm.Categories = await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).ToListAsync();
        vm.TeamMembers = IsManagerOrAbove ? await GetTeamMembersAsync() : new List<User>();
        return View(vm);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var task = await Context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == CurrentOrganizationId);
        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return NotFound();

        var primaryAssignee = task.Assignees.FirstOrDefault(a => a.IsPrimary);

        var vm = new TaskViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CategoryId = task.CategoryId,
            AssigneeId = primaryAssignee?.UserId,
            Categories = await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).ToListAsync(),
            TeamMembers = IsManagerOrAbove ? await GetTeamMembersAsync() : new List<User>()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaskViewModel vm)
    {
        if (id != vm.Id) return NotFound();

        var task = await Context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == CurrentOrganizationId);
        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return NotFound();

        if (ModelState.IsValid)
        {
            var oldTitle = task.Title;
            var oldStatus = task.Status;
            var oldPriority = task.Priority;
            var oldDueDate = task.DueDate;
            var oldCategoryId = task.CategoryId;
            var oldAssigneeId = task.Assignees.FirstOrDefault(a => a.IsPrimary)?.UserId;

            task.Title = vm.Title;
            task.Description = vm.Description;
            task.Status = vm.Status;
            task.Priority = vm.Priority;
            task.DueDate = vm.DueDate;
            task.CategoryId = vm.CategoryId;
            task.UpdatedAt = DateTime.UtcNow;

            if (oldTitle != vm.Title)
                await LogActivityAsync(task.Id, "Title", oldTitle, vm.Title);
            if ((task.Description ?? "") != (vm.Description ?? ""))
                await LogActivityAsync(task.Id, "Description", task.Description, vm.Description);
            if (oldStatus != vm.Status)
                await LogActivityAsync(task.Id, "Status", oldStatus.ToString(), vm.Status.ToString());
            if (oldPriority != vm.Priority)
                await LogActivityAsync(task.Id, "Priority", oldPriority.ToString(), vm.Priority.ToString());
            if (oldDueDate != vm.DueDate)
                await LogActivityAsync(task.Id, "DueDate", oldDueDate?.ToString("yyyy-MM-dd"), vm.DueDate?.ToString("yyyy-MM-dd"));
            if (oldCategoryId != vm.CategoryId)
            {
                var oldCat = oldCategoryId.HasValue ? (await Context.Categories.FindAsync(oldCategoryId))?.Name : null;
                var newCat = vm.CategoryId.HasValue ? (await Context.Categories.FindAsync(vm.CategoryId))?.Name : null;
                await LogActivityAsync(task.Id, "Category", oldCat, newCat);
            }

            if (IsManagerOrAbove && vm.AssigneeId.HasValue && vm.AssigneeId.Value != oldAssigneeId)
            {
                Context.TaskAssignees.RemoveRange(task.Assignees);
                Context.TaskAssignees.Add(new TaskAssignee
                {
                    TaskItemId = task.Id,
                    UserId = vm.AssigneeId.Value,
                    IsPrimary = true
                });
                var userName = await Context.Users.Where(u => u.Id == vm.AssigneeId.Value).Select(u => u.Name).FirstOrDefaultAsync();
                await LogActivityAsync(task.Id, "Assignee", oldAssigneeId.HasValue ? (await Context.Users.Where(u => u.Id == oldAssigneeId.Value).Select(u => u.Name).FirstOrDefaultAsync()) : null, userName);
                await CreateNotificationAsync(vm.AssigneeId.Value, $"{CurrentUserName} assigned you to \"{task.Title}\"", $"/Tasks/Details/{task.Id}");
            }

            await Context.SaveChangesAsync();
            TempData["Success"] = "Task updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        vm.Categories = await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).ToListAsync();
        vm.TeamMembers = IsManagerOrAbove ? await GetTeamMembersAsync() : new List<User>();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, TaskItemStatus status)
    {
        var task = await Context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == CurrentOrganizationId);
        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return NotFound();

        var oldStatus = task.Status;
        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        if (oldStatus != status)
            await LogActivityAsync(task.Id, "Status", oldStatus.ToString(), status.ToString());

        TempData["Success"] = $"Task status updated to {status}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var task = await Context.Tasks
            .Include(t => t.Category)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == CurrentOrganizationId);
        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return NotFound();

        return View(task);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var task = await Context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == CurrentOrganizationId);
        if (task == null) return NotFound();

        var isAssigned = task.Assignees.Any(a => a.UserId == CurrentUserId);
        if (!IsManagerOrAbove && !isAssigned)
            return NotFound();

        Context.TaskAssignees.RemoveRange(task.Assignees);
        Context.Tasks.Remove(task);
        await Context.SaveChangesAsync();

        TempData["Success"] = "Task deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserName => User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown";

    private async Task LogActivityAsync(int taskId, string fieldName, string? oldValue, string? newValue)
    {
        Context.TaskActivityLogs.Add(new TaskActivityLog
        {
            TaskItemId = taskId,
            UserId = CurrentUserId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();
    }

    private async Task CreateNotificationAsync(int userId, string message, string? link = null)
    {
        Context.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow
        });
        await Context.SaveChangesAsync();
    }
}
