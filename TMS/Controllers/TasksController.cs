using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TMS.Data;
using TMS.Helpers;
using TMS.Models;
using TMS.Services;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize]
public class TasksController : BaseController
{
    private readonly ITaskService _taskService;
    private readonly ITeamService _teamService;

    public TasksController(AppDbContext context, ITaskService taskService, ITeamService teamService) : base(context)
    {
        _taskService = taskService;
        _teamService = teamService;
    }

    /// <summary>
    /// Displays the kanban board view of tasks grouped by status.
    /// </summary>
    /// <returns>The board view with a BoardViewModel.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Board()
    {
        BoardViewModel vm = await _taskService.GetBoardAsync(CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        return View(vm);
    }

    /// <summary>
    /// Updates the board position (column and order) of a task via drag-and-drop.
    /// </summary>
    /// <param name="taskId">The ID of the task to move.</param>
    /// <param name="columnId">The target column (status) ID.</param>
    /// <param name="order">The new display order within the column.</param>
    /// <returns>HTTP 200 OK on success.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBoardPosition(int taskId, int columnId, int order)
    {
        await _taskService.UpdateBoardPositionAsync(taskId, columnId, order, CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        return Ok();
    }

    /// <summary>
    /// Displays the paginated, filterable list of tasks with search, status, category, and sort options.
    /// </summary>
    /// <param name="search">Optional search keyword for task titles.</param>
    /// <param name="status">Optional task status filter.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="sort">Optional sort field.</param>
    /// <param name="page">The current page number (default 1).</param>
    /// <param name="pageSize">The number of tasks per page (default 10).</param>
    /// <returns>A view with the filtered list of tasks and pagination data in ViewBag.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index(string? search, TaskItemStatus? status, int? categoryId, string? sort, int page = 1, int pageSize = 10)
    {
        PaginatedList<TaskItem> result = await _taskService.GetFilteredTasksAsync(
            CurrentOrganizationId, CurrentUserId, IsManagerOrAbove,
            search, status, categoryId, sort, page, pageSize);

        ViewBag.Search = search;
        ViewBag.StatusFilter = status;
        ViewBag.CategoryFilter = categoryId;
        ViewBag.Sort = sort;
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.Categories = new SelectList(await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).AsNoTracking().ToListAsync(), "Id", "Name", categoryId);
        ViewBag.Statuses = new SelectList(Enum.GetValues<TaskItemStatus>().Cast<TaskItemStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }), "Value", "Text", status.HasValue ? (int)status.Value : null);
        ViewBag.IsManager = IsManagerOrAbove;

        return View(result.Items);
    }

    /// <summary>
    /// Displays the details of a specific task including comments and assignees.
    /// </summary>
    /// <param name="id">The ID of the task to display.</param>
    /// <returns>The task details view, or NotFound if the task does not exist.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        TaskItem? task = await _taskService.GetTaskDetailsAsync(id.Value, CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        if (task == null) return NotFound();

        return View(task);
    }

    /// <summary>
    /// Handles adding a comment to a task.
    /// </summary>
    /// <param name="taskId">The ID of the task to comment on.</param>
    /// <param name="content">The comment text content.</param>
    /// <param name="parentCommentId">Optional ID of a parent comment for replies.</param>
    /// <returns>Redirects to the task details page after adding the comment.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> AddComment(int taskId, string content, int? parentCommentId)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        string userName = CurrentUserName;
        await _taskService.AddCommentAsync(taskId, content, parentCommentId, CurrentUserId, CurrentOrganizationId, IsManagerOrAbove, userName);

        TempData["Success"] = "Comment added.";
        return RedirectToAction(nameof(Details), new { id = taskId });
    }

    /// <summary>
    /// Displays the task creation form with category and team member selections.
    /// </summary>
    /// <returns>The create task view with a TaskViewModel.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create()
    {
        TaskViewModel vm = await _taskService.GetCreateModelAsync(CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        return View(vm);
    }

    /// <summary>
    /// Handles the creation of a new task.
    /// </summary>
    /// <param name="vm">The task view model containing the new task data.</param>
    /// <returns>Redirects to the task index on success, or returns the create view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Create(TaskViewModel vm)
    {
        if (ModelState.IsValid)
        {
            await _taskService.CreateTaskAsync(vm, CurrentUserId, CurrentOrganizationId, IsManagerOrAbove);
            TempData["Success"] = "Task created successfully!";
            return RedirectToAction(nameof(Index));
        }

        vm.Categories = await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).ToListAsync();
        vm.TeamMembers = IsManagerOrAbove ? await _teamService.GetTeamMembersAsync(CurrentOrganizationId, CurrentUserId) : new List<User>();
        return View(vm);
    }

    /// <summary>
    /// Displays the task edit form for the specified task.
    /// </summary>
    /// <param name="id">The ID of the task to edit.</param>
    /// <returns>The edit task view with a TaskViewModel, or NotFound if the task does not exist.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        TaskViewModel? vm = await _taskService.GetEditModelAsync(id.Value, CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        if (vm == null) return NotFound();

        return View(vm);
    }

    /// <summary>
    /// Handles the update of an existing task.
    /// </summary>
    /// <param name="id">The ID of the task to update.</param>
    /// <param name="vm">The task view model with updated data.</param>
    /// <returns>Redirects to the task index on success, returns the edit view with validation errors, or NotFound if IDs do not match.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(int id, TaskViewModel vm)
    {
        if (id != vm.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _taskService.UpdateTaskAsync(id, vm, CurrentUserId, CurrentOrganizationId, IsManagerOrAbove);
            TempData["Success"] = "Task updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        vm.Categories = await Context.Categories.Where(c => c.OrganizationId == CurrentOrganizationId).ToListAsync();
        vm.TeamMembers = IsManagerOrAbove ? await _teamService.GetTeamMembersAsync(CurrentOrganizationId, CurrentUserId) : new List<User>();
        return View(vm);
    }

    /// <summary>
    /// Handles updating the status of a task.
    /// </summary>
    /// <param name="id">The ID of the task to update.</param>
    /// <param name="status">The new task status.</param>
    /// <returns>Redirects to the task index after updating the status.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> UpdateStatus(int id, TaskItemStatus status)
    {
        await _taskService.UpdateStatusAsync(id, status, CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        TempData["Success"] = $"Task status updated to {status}";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Displays the delete confirmation page for a task.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <returns>The delete confirmation view with the task details, or NotFound if the task does not exist.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        TaskItem? task = await _taskService.GetDeleteModelAsync(id.Value, CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        if (task == null) return NotFound();

        return View(task);
    }

    /// <summary>
    /// Handles the deletion of a task after confirmation.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <returns>Redirects to the task index after deletion.</returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _taskService.DeleteTaskAsync(id, CurrentOrganizationId, CurrentUserId, IsManagerOrAbove);
        TempData["Success"] = "Task deleted successfully!";
        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserName => User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown";
}
