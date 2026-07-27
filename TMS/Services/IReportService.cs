using TMS.Models;
using TMS.ViewModels;

namespace TMS.Services;

/// <summary>Defines methods for generating organizational reports.</summary>
public interface IReportService
{
    /// <summary>Generates a team report with efficiency metrics for the organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <returns>A view model containing team summary, employee efficiency data, and chart data.</returns>
    Task<TeamReportViewModel> GetTeamReportAsync(int orgId);
    /// <summary>Retrieves detailed task information for a specific employee within an organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="employeeId">The employee user identifier.</param>
    /// <returns>A tuple containing the employee's tasks, name, and avatar URL.</returns>
    Task<(List<TaskItem> Tasks, string EmployeeName, string AvatarUrl)> GetEmployeeDetailAsync(int orgId, int employeeId);
}
