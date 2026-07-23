using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Password { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public ICollection<OrganizationMembership> OrganizationMemberships { get; set; } = new List<OrganizationMembership>();

    public ICollection<TeamMembership> TeamMemberships { get; set; } = new List<TeamMembership>();

    public ICollection<TaskAssignee> TaskAssignments { get; set; } = new List<TaskAssignee>();

    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

    public ICollection<TaskActivityLog> ActivityLogs { get; set; } = new List<TaskActivityLog>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
