using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Helpers;
using TMS.Models;
using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Provides implementations for managing tasks, board positions, comments, and task metadata.</summary>
public class TaskService : ITaskService
{
    private readonly AppDbContext _context;
    private readonly ITeamService _teamService;
    private readonly INotificationService _notificationService;

    /// <summary>Initializes a new instance of the <see cref="TaskService"/> class.</summary>
    /// <param name="context">The database context.</param>
    /// <param name="teamService">The team service for retrieving team members.</param>
    /// <param name="notificationService">The notification service for sending task notifications.</param>
    public TaskService(AppDbContext context, ITeamService teamService, INotificationService notificationService)
    {
        _context = context;
        _teamService = teamService;
        _notificationService = notificationService;
    }

    /// <summary>Retrieves the board view with columns and tasks for the organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>A view model containing the board name, project name, and columns with tasks.</returns>
    public async Task<BoardViewModel> GetBoardAsync(int orgId, int userId, bool isManagerOrAbove)
    {
        List<BoardColumn> columns = await _context.BoardColumns
            .Where(bc => bc.Board.Project.OrganizationId == orgId)
            .Include(bc => bc.Tasks.Where(t => isManagerOrAbove || t.Assignees.Any(a => a.UserId == userId)))
                .ThenInclude(t => t.Assignees).ThenInclude(a => a.User)
            .Include(bc => bc.Tasks).ThenInclude(t => t.Category)
            .OrderBy(bc => bc.Order)
            .ToListAsync();

        Board? board = await _context.Boards
            .Include(b => b.Project)
            .FirstOrDefaultAsync(b => b.Project.OrganizationId == orgId);

        return new BoardViewModel
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
    }

    /// <summary>Updates the board column and display order of a task, with access checks.</summary>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="columnId">The target column identifier.</param>
    /// <param name="order">The new display order within the column.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    public async Task UpdateBoardPositionAsync(int taskId, int columnId, int order, int orgId, int userId, bool isManagerOrAbove)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == orgId);

        if (task == null) return;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return;

        BoardColumn? column = await _context.BoardColumns
            .FirstOrDefaultAsync(bc => bc.Id == columnId && bc.Board.Project.OrganizationId == orgId);

        if (column == null) return;

        task.BoardColumnId = columnId;
        task.BoardOrder = order;
        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    /// <summary>Retrieves a paginated, filtered, and sorted list of tasks for the given organization and user.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <param name="search">An optional search string to filter by title or description.</param>
    /// <param name="status">An optional status filter.</param>
    /// <param name="categoryId">An optional category filter.</param>
    /// <param name="sort">An optional sort expression (dueDate, dueDateDesc, priority, created).</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated list of task items.</returns>
    public async Task<PaginatedList<TaskItem>> GetFilteredTasksAsync(
        int orgId, int userId, bool isManagerOrAbove, string? search,
        TaskItemStatus? status, int? categoryId, string? sort, int page, int pageSize)
    {
        IQueryable<TaskItem> query;

        if (isManagerOrAbove)
        {
            query = _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Assignees).ThenInclude(a => a.User)
                .Where(t => t.OrganizationId == orgId);
        }
        else
        {
            query = _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.Assignees).ThenInclude(a => a.User)
                .Where(t => t.OrganizationId == orgId && t.Assignees.Any(a => a.UserId == userId));
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        query = sort switch
        {
            "dueDate" => query.OrderBy(t => t.DueDate),
            "dueDateDesc" => query.OrderByDescending(t => t.DueDate),
            "priority" => query.OrderByDescending(t => t.Priority),
            "created" => query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        return await PaginatedList<TaskItem>.CreateAsync(query, page, pageSize);
    }

    /// <summary>Retrieves full task details including category, assignees, comments, replies, and activity logs.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>The task with all related data if found and accessible; otherwise <c>null</c>.</returns>
    public async Task<TaskItem?> GetTaskDetailsAsync(int id, int orgId, int userId, bool isManagerOrAbove)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Include(t => t.Comments).ThenInclude(c => c.User)
            .Include(t => t.Comments).ThenInclude(c => c.Replies).ThenInclude(r => r.User)
            .Include(t => t.ActivityLogs).ThenInclude(al => al.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);

        if (task == null) return null;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return null;

        return task;
    }

    /// <summary>Adds a comment (or reply) to a task, logs the activity, and notifies assignees.</summary>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="content">The comment text.</param>
    /// <param name="parentCommentId">An optional parent comment identifier for replies.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <param name="userName">The display name of the comment author.</param>
    public async Task AddCommentAsync(int taskId, string content, int? parentCommentId, int userId, int orgId, bool isManagerOrAbove, string userName)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OrganizationId == orgId);

        if (task == null) return;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return;

        if (string.IsNullOrWhiteSpace(content)) return;

        var comment = new TaskComment
        {
            TaskItemId = taskId,
            UserId = userId,
            Content = content.Trim(),
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TaskComments.Add(comment);
        await _context.SaveChangesAsync();

        await LogActivityAsync(taskId, "Comment", null, "Added a comment", userId);

        var assigneeIds = task.Assignees.Where(a => a.UserId != userId).Select(a => a.UserId).ToList();
        foreach (int uid in assigneeIds)
        {
            await _notificationService.CreateNotificationAsync(uid, $"{userName} commented on \"{task.Title}\"", $"/Tasks/Details/{taskId}");
        }
    }

    /// <summary>Retrieves the view model for creating a new task, including available categories and team members.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>A task view model populated with form data.</returns>
    public async Task<TaskViewModel> GetCreateModelAsync(int orgId, int userId, bool isManagerOrAbove)
    {
        return new TaskViewModel
        {
            Categories = await _context.Categories.Where(c => c.OrganizationId == orgId).ToListAsync(),
            TeamMembers = isManagerOrAbove ? await _teamService.GetTeamMembersAsync(orgId, userId) : new List<User>(),
            AssigneeId = isManagerOrAbove ? null : userId
        };
    }

    /// <summary>Creates a new task, assigns it to a user, logs the creation activity, and sends a notification to the assignee.</summary>
    /// <param name="vm">The task view model containing task data.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    public async Task CreateTaskAsync(TaskViewModel vm, int userId, int orgId, bool isManagerOrAbove)
    {
        var task = new TaskItem
        {
            Title = vm.Title,
            Description = vm.Description,
            Status = vm.Status,
            Priority = vm.Priority,
            DueDate = vm.DueDate,
            CategoryId = vm.CategoryId,
            UserId = userId,
            OrganizationId = orgId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Add(task);
        await _context.SaveChangesAsync();

        int assigneeId = isManagerOrAbove ? (vm.AssigneeId ?? userId) : userId;

        _context.TaskAssignees.Add(new TaskAssignee
        {
            TaskItemId = task.Id,
            UserId = assigneeId,
            IsPrimary = true
        });
        await _context.SaveChangesAsync();

        await LogActivityAsync(task.Id, "Task", null, $"Created task \"{task.Title}\"", userId);

        if (assigneeId != userId)
        {
            string? userName = await _context.Users.Where(u => u.Id == assigneeId).Select(u => u.Name).FirstOrDefaultAsync();
            await _notificationService.CreateNotificationAsync(assigneeId, $"{GetCurrentUserName(userId)} assigned you to \"{task.Title}\"", $"/Tasks/Details/{task.Id}");
        }
    }

    /// <summary>Retrieves the view model for editing an existing task, including current values and available options.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>The task view model if found and accessible; otherwise <c>null</c>.</returns>
    public async Task<TaskViewModel?> GetEditModelAsync(int id, int orgId, int userId, bool isManagerOrAbove)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
        if (task == null) return null;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return null;

        TaskAssignee? primaryAssignee = task.Assignees.FirstOrDefault(a => a.IsPrimary);

        return new TaskViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CategoryId = task.CategoryId,
            AssigneeId = primaryAssignee?.UserId,
            Categories = await _context.Categories.Where(c => c.OrganizationId == orgId).ToListAsync(),
            TeamMembers = isManagerOrAbove ? await _teamService.GetTeamMembersAsync(orgId, userId) : new List<User>()
        };
    }

    /// <summary>Updates an existing task, logs field-level changes, manages assignee changes, and notifies new assignees.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="vm">The updated task view model.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    public async Task UpdateTaskAsync(int id, TaskViewModel vm, int userId, int orgId, bool isManagerOrAbove)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
        if (task == null) return;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return;

        string oldTitle = task.Title;
        TaskItemStatus oldStatus = task.Status;
        TaskPriority oldPriority = task.Priority;
        DateTime? oldDueDate = task.DueDate;
        int? oldCategoryId = task.CategoryId;
        int? oldAssigneeId = task.Assignees.FirstOrDefault(a => a.IsPrimary)?.UserId;

        task.Title = vm.Title;
        task.Description = vm.Description;
        task.Status = vm.Status;
        task.Priority = vm.Priority;
        task.DueDate = vm.DueDate;
        task.CategoryId = vm.CategoryId;
        task.UpdatedAt = DateTime.UtcNow;

        if (oldTitle != vm.Title)
            await LogActivityAsync(task.Id, "Title", oldTitle, vm.Title, userId);
        if ((task.Description ?? "") != (vm.Description ?? ""))
            await LogActivityAsync(task.Id, "Description", task.Description, vm.Description, userId);
        if (oldStatus != vm.Status)
            await LogActivityAsync(task.Id, "Status", oldStatus.ToString(), vm.Status.ToString(), userId);
        if (oldPriority != vm.Priority)
            await LogActivityAsync(task.Id, "Priority", oldPriority.ToString(), vm.Priority.ToString(), userId);
        if (oldDueDate != vm.DueDate)
            await LogActivityAsync(task.Id, "DueDate", oldDueDate?.ToString("yyyy-MM-dd"), vm.DueDate?.ToString("yyyy-MM-dd"), userId);
        if (oldCategoryId != vm.CategoryId)
        {
            string? oldCat = oldCategoryId.HasValue ? (await _context.Categories.FindAsync(oldCategoryId))?.Name : null;
            string? newCat = vm.CategoryId.HasValue ? (await _context.Categories.FindAsync(vm.CategoryId))?.Name : null;
            await LogActivityAsync(task.Id, "Category", oldCat, newCat, userId);
        }

        if (isManagerOrAbove && vm.AssigneeId.HasValue && vm.AssigneeId.Value != oldAssigneeId)
        {
            _context.TaskAssignees.RemoveRange(task.Assignees);
            _context.TaskAssignees.Add(new TaskAssignee
            {
                TaskItemId = task.Id,
                UserId = vm.AssigneeId.Value,
                IsPrimary = true
            });
            string? userName = await _context.Users.Where(u => u.Id == vm.AssigneeId.Value).Select(u => u.Name).FirstOrDefaultAsync();
            await LogActivityAsync(task.Id, "Assignee", oldAssigneeId.HasValue ? (await _context.Users.Where(u => u.Id == oldAssigneeId.Value).Select(u => u.Name).FirstOrDefaultAsync()) : null, userName, userId);
            await _notificationService.CreateNotificationAsync(vm.AssigneeId.Value, $"{GetCurrentUserName(userId)} assigned you to \"{task.Title}\"", $"/Tasks/Details/{task.Id}");
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>Updates the status of a task and logs the status change.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="status">The new status value.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    public async Task UpdateStatusAsync(int id, TaskItemStatus status, int orgId, int userId, bool isManagerOrAbove)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
        if (task == null) return;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return;

        TaskItemStatus oldStatus = task.Status;
        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (oldStatus != status)
            await LogActivityAsync(task.Id, "Status", oldStatus.ToString(), status.ToString(), userId);
    }

    /// <summary>Retrieves a task for deletion confirmation, including its category and assignees.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>The task if found and accessible; otherwise <c>null</c>.</returns>
    public async Task<TaskItem?> GetDeleteModelAsync(int id, int orgId, int userId, bool isManagerOrAbove)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Category)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
        if (task == null) return null;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return null;

        return task;
    }

    /// <summary>Deletes a task and its associated assignee records.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    public async Task DeleteTaskAsync(int id, int orgId, int userId, bool isManagerOrAbove)
    {
        TaskItem? task = await _context.Tasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == orgId);
        if (task == null) return;

        bool isAssigned = task.Assignees.Any(a => a.UserId == userId);
        if (!isManagerOrAbove && !isAssigned) return;

        _context.TaskAssignees.RemoveRange(task.Assignees);
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    private async Task LogActivityAsync(int taskId, string fieldName, string? oldValue, string? newValue, int userId)
    {
        _context.TaskActivityLogs.Add(new TaskActivityLog
        {
            TaskItemId = taskId,
            UserId = userId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private string GetCurrentUserName(int userId)
    {
        User? user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);
        return user?.Name ?? "Unknown";
    }
}
