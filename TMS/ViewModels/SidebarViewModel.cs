namespace TMS.ViewModels;

public class SidebarViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public List<SidebarCategory> Categories { get; set; } = new();
    public string CurrentController { get; set; } = string.Empty;
    public string CurrentAction { get; set; } = string.Empty;
    public string CurrentCategoryId { get; set; } = string.Empty;
}

public class SidebarCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}
