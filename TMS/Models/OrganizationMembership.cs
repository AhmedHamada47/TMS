using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models;

public class OrganizationMembership
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization Organization { get; set; } = null!;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public OrganizationRole Role { get; set; } = OrganizationRole.Employee;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public enum OrganizationRole
{
    Employee,
    TeamLead,
    Manager,
    Admin
}
