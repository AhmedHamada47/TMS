using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models;

public class TaskAssignee
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    [ForeignKey(nameof(TaskItemId))]
    public TaskItem TaskItem { get; set; } = null!;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public bool IsPrimary { get; set; }
}
