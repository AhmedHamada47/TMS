using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models;

public class TeamMembership
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    [ForeignKey(nameof(TeamId))]
    public Team Team { get; set; } = null!;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public TeamRole Role { get; set; } = TeamRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public enum TeamRole
{
    Member,
    Lead
}
