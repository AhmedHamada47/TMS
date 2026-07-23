using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models;

public class TaskComment
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    [ForeignKey(nameof(TaskItemId))]
    public TaskItem TaskItem { get; set; } = null!;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public TaskComment? ParentComment { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaskComment> Replies { get; set; } = new List<TaskComment>();
}
