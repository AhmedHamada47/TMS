using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models;

public class TaskItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DueDate { get; set; }

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public int OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public int? BoardColumnId { get; set; }

    public BoardColumn? BoardColumn { get; set; }

    public int BoardOrder { get; set; }

    public decimal? EstimatedHours { get; set; }

    public decimal? ActualHours { get; set; }

    public ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    public ICollection<TaskActivityLog> ActivityLogs { get; set; } = new List<TaskActivityLog>();
}

public enum TaskItemStatus
{
    ToDo,
    InProgress,
    Done
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}
