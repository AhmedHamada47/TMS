using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS.Data;
using TMS.Services;
using TMS.ViewModels;

namespace TMS.Controllers;

[Authorize(Policy = "ManagerOrAbove")]
public class ReportsController : BaseController
{
    private readonly IReportService _reportService;

    public ReportsController(AppDbContext context, IReportService reportService) : base(context)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Displays the team report dashboard with task statistics per employee.
    /// </summary>
    /// <returns>A view with the team report view model.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Index()
    {
        TeamReportViewModel vm = await _reportService.GetTeamReportAsync(CurrentOrganizationId);
        return View(vm);
    }

    /// <summary>
    /// Displays the detailed task report for a specific employee.
    /// </summary>
    /// <param name="id">The employee's user ID.</param>
    /// <returns>The employee detail view with a list of tasks, or NotFound if the employee has no tasks.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EmployeeDetail(int id)
    {
        (List<Models.TaskItem>? tasks, string? employeeName, string? avatarUrl) = await _reportService.GetEmployeeDetailAsync(CurrentOrganizationId, id);

        if (tasks.Count == 0 && string.IsNullOrEmpty(employeeName))
            return NotFound();

        ViewBag.EmployeeName = employeeName;
        ViewBag.AvatarUrl = avatarUrl;

        return View(tasks);
    }
}
