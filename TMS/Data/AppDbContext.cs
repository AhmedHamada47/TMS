using Microsoft.EntityFrameworkCore;
using TMS.Models;

namespace TMS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasOne(t => t.Category)
                  .WithMany(c => c.Tasks)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(t => t.User)
                  .WithMany(u => u.Tasks)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);
            entity.HasIndex(t => t.DueDate);
            entity.HasIndex(t => t.CategoryId);
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.CreatedAt);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.Email).IsUnique();
        });

        SeedData(modelBuilder);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var pwd = HashPassword("123456");
        var users = new List<User>
        {
            new() { Id = 1, Name = "Ahmed Hamada", Email = "ahmed@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png" },
            new() { Id = 2, Name = "Sara Ali", Email = "sara@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Female/Number_47_ssmlmw_zlydth.png" },
            new() { Id = 3, Name = "Mohamed Hassan", Email = "mohamed@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png" },
        };

        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Development", Description = "Software development tasks", Color = "#0d6efd" },
            new() { Id = 2, Name = "Design", Description = "UI/UX design tasks", Color = "#6f42c1" },
            new() { Id = 3, Name = "Marketing", Description = "Marketing and promotion tasks", Color = "#198754" },
            new() { Id = 4, Name = "Meeting", Description = "Meetings and coordination", Color = "#fd7e14" },
            new() { Id = 5, Name = "Research", Description = "Research and analysis tasks", Color = "#dc3545" },
        };

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Set up project structure", Description = "Initialize the ASP.NET Core MVC project with proper folder structure", Status = Models.TaskItemStatus.Done, Priority = TaskPriority.High, CategoryId = 1, UserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-10), DueDate = DateTime.UtcNow.AddDays(-8) },
            new() { Id = 2, Title = "Design database schema", Description = "Create EF Core models and configure relationships", Status = Models.TaskItemStatus.Done, Priority = TaskPriority.High, CategoryId = 1, UserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-8), DueDate = DateTime.UtcNow.AddDays(-5) },
            new() { Id = 3, Title = "Create dashboard UI", Description = "Build an attractive dashboard with task statistics", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.High, CategoryId = 2, UserId = 2, CreatedAt = DateTime.UtcNow.AddDays(-5), DueDate = DateTime.UtcNow.AddDays(2) },
            new() { Id = 4, Title = "Implement task CRUD", Description = "Full CRUD operations for task management", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.Urgent, CategoryId = 1, UserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-4), DueDate = DateTime.UtcNow.AddDays(1) },
            new() { Id = 5, Title = "Write unit tests", Description = "Cover controllers and services with unit tests", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Medium, CategoryId = 1, UserId = 3, CreatedAt = DateTime.UtcNow.AddDays(-3), DueDate = DateTime.UtcNow.AddDays(7) },
            new() { Id = 6, Title = "Marketing campaign Q3", Description = "Plan and execute Q3 marketing campaign", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Medium, CategoryId = 3, UserId = 2, CreatedAt = DateTime.UtcNow.AddDays(-2), DueDate = DateTime.UtcNow.AddDays(14) },
            new() { Id = 7, Title = "Sprint planning meeting", Description = "Plan next sprint with the team", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Low, CategoryId = 4, UserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-1), DueDate = DateTime.UtcNow.AddDays(5) },
            new() { Id = 8, Title = "Competitor analysis", Description = "Research competitors and compile report", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.Medium, CategoryId = 5, UserId = 3, CreatedAt = DateTime.UtcNow.AddDays(-6), DueDate = DateTime.UtcNow.AddDays(3) },
        };

        modelBuilder.Entity<User>().HasData(users);
        modelBuilder.Entity<Category>().HasData(categories);
        modelBuilder.Entity<TaskItem>().HasData(tasks);
    }
}
