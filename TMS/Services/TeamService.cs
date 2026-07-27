using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;

namespace TMS.Services;

/// <summary>Provides implementations for retrieving team member information.</summary>
public class TeamService : ITeamService
{
    private readonly AppDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="TeamService"/> class.</summary>
    /// <param name="context">The database context.</param>
    public TeamService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Retrieves the distinct team members for all teams the specified user belongs to within an organization.</summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="userId">The user identifier whose teams are queried.</param>
    /// <returns>A list of distinct users who share a team with the given user.</returns>
    public async Task<List<User>> GetTeamMembersAsync(int orgId, int userId)
    {
        List<int> teamIds = await _context.TeamMemberships
            .Where(tm => tm.Team.OrganizationId == orgId && tm.UserId == userId)
            .Select(tm => tm.TeamId)
            .ToListAsync();

        if (teamIds.Count == 0)
            return new List<User>();

        return await _context.TeamMemberships
            .Where(tm => teamIds.Contains(tm.TeamId))
            .Select(tm => tm.User)
            .Distinct()
            .AsNoTracking()
            .ToListAsync();
    }
}
