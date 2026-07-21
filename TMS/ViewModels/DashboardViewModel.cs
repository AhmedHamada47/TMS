using TMS.Models;

namespace TMS.ViewModels;

public class DashboardViewModel
{
    public int TotalTasks { get; set; }
    public int TasksToDo { get; set; }
    public int TasksInProgress { get; set; }
    public int TasksDone { get; set; }
    public int UrgentTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int TotalUsers { get; set; }
    public int TotalCategories { get; set; }
    public List<TaskItem> RecentTasks { get; set; } = new();
    public List<TaskItem> UpcomingTasks { get; set; } = new();
    public List<CategorySummary> CategorySummaries { get; set; } = new();
}

public class CategorySummary
{
    public string CategoryName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}
