namespace TMS.ViewModels;

public class ActivityChartViewModel
{
    public List<DailyActivity> DailyData { get; set; } = new();
    public int CurrentStreak { get; set; }
    public int TasksCompletedThisWeek { get; set; }
    public double CompletionRate { get; set; }
}

public class DailyActivity
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}
