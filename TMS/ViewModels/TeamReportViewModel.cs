using TMS.Models;

namespace TMS.ViewModels;

public class TeamReportViewModel
{
    public TeamSummary TeamSummary { get; set; } = new();
    public List<EmployeeEfficiency> Employees { get; set; } = new();
    public List<string> ChartLabels { get; set; } = new();
    public List<double> ChartCompletionRates { get; set; } = new();
    public List<double> ChartOnTimeRates { get; set; } = new();
}

public class TeamSummary
{
    public string TeamName { get; set; } = string.Empty;
    public int TotalMembers { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double AvgCompletionRate { get; set; }
}

public class EmployeeEfficiency
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionRate { get; set; }
    public int OnTimeTasks { get; set; }
    public double OnTimeRate { get; set; }
    public double AvgCycleHours { get; set; }
    public int OverdueCount { get; set; }
    public int UrgentCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
}
