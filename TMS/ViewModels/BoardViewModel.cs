using TMS.Models;

namespace TMS.ViewModels;

public class BoardViewModel
{
    public string BoardName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public List<BoardColumnViewModel> Columns { get; set; } = new();
}

public class BoardColumnViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TaskItem> Tasks { get; set; } = new();
}
