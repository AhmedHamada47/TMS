using TMS.Models;

namespace TMS.Services;

/// <summary>Defines methods for retrieving team member information.</summary>
public interface ITeamService
{
    /// <summary>Retrieves the distinct team members for all teams the specified user belongs to within an organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The user identifier whose teams are queried.</param>
    /// <returns>A list of distinct users who share a team with the given user.</returns>
    Task<List<User>> GetTeamMembersAsync(int orgId, int userId);
}
