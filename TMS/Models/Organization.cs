using System.ComponentModel.DataAnnotations;

namespace TMS.Models;

public class Organization
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();
}
