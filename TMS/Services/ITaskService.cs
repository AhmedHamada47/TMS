using TMS.Models;
using TMS.Helpers;
using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Defines methods for managing tasks, board positions, comments, and task metadata.</summary>
public interface ITaskService
{
    /// <summary>Retrieves the board view with columns and tasks for the organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>A view model containing the board name, project name, and columns with tasks.</returns>
    Task<BoardViewModel> GetBoardAsync(int orgId, int userId, bool isManagerOrAbove);
    /// <summary>Updates the board column and order position of a task.</summary>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="columnId">The target column identifier.</param>
    /// <param name="order">The new display order within the column.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    Task UpdateBoardPositionAsync(int taskId, int columnId, int order, int orgId, int userId, bool isManagerOrAbove);
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
    Task<PaginatedList<TaskItem>> GetFilteredTasksAsync(int orgId, int userId, bool isManagerOrAbove, string? search, TaskItemStatus? status, int? categoryId, string? sort, int page, int pageSize);
    /// <summary>Retrieves full task details including category, assignees, comments, replies, and activity logs.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>The task with all related data if found and accessible; otherwise <c>null</c>.</returns>
    Task<TaskItem?> GetTaskDetailsAsync(int id, int orgId, int userId, bool isManagerOrAbove);
    /// <summary>Adds a comment (or reply) to a task and notifies assignees.</summary>
    /// <param name="taskId">The task identifier.</param>
    /// <param name="content">The comment text.</param>
    /// <param name="parentCommentId">An optional parent comment identifier for replies.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <param name="userName">The display name of the comment author.</param>
    Task AddCommentAsync(int taskId, string content, int? parentCommentId, int userId, int orgId, bool isManagerOrAbove, string userName);
    /// <summary>Retrieves the view model for creating a new task, including available categories and team members.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>A task view model populated with form data.</returns>
    Task<TaskViewModel> GetCreateModelAsync(int orgId, int userId, bool isManagerOrAbove);
    /// <summary>Creates a new task, assigns it to a user, and sends a notification to the assignee.</summary>
    /// <param name="vm">The task view model containing task data.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    Task CreateTaskAsync(TaskViewModel vm, int userId, int orgId, bool isManagerOrAbove);
    /// <summary>Retrieves the view model for editing an existing task.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>The task view model if found and accessible; otherwise <c>null</c>.</returns>
    Task<TaskViewModel?> GetEditModelAsync(int id, int orgId, int userId, bool isManagerOrAbove);
    /// <summary>Updates an existing task, logs field-level changes, and notifies new assignees.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="vm">The updated task view model.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    Task UpdateTaskAsync(int id, TaskViewModel vm, int userId, int orgId, bool isManagerOrAbove);
    /// <summary>Updates the status of a task and logs the change.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="status">The new status value.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    Task UpdateStatusAsync(int id, TaskItemStatus status, int orgId, int userId, bool isManagerOrAbove);
    /// <summary>Retrieves a task for deletion confirmation, including its category and assignees.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    /// <returns>The task if found and accessible; otherwise <c>null</c>.</returns>
    Task<TaskItem?> GetDeleteModelAsync(int id, int orgId, int userId, bool isManagerOrAbove);
    /// <summary>Deletes a task and its assignee records.</summary>
    /// <param name="id">The task identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The current user identifier.</param>
    /// <param name="isManagerOrAbove">Whether the user has manager-level permissions or higher.</param>
    Task DeleteTaskAsync(int id, int orgId, int userId, bool isManagerOrAbove);
}
