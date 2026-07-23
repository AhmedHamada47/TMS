using System.ComponentModel.DataAnnotations;
using TMS.Models;

namespace TMS.ViewModels;

public class TaskViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    public int? CategoryId { get; set; }

    public int? AssigneeId { get; set; }

    public List<Category> Categories { get; set; } = new();
    public Category? Category { get; set; }
    public List<User> TeamMembers { get; set; } = new();
}
