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
            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.Email).IsUnique();
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var pwd = "$2a$11$im1itOCIomo1FryxxRewfOz9dCxfJ6eFBTpoOLiTK/ZAJvYtkUExm";
        var users = new List<User>
        {
            new() { Id = 1, Name = "Ahmed Hamada", Email = "ahmed@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png", CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Name = "Sara Ali", Email = "sara@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Female/Number_47_ssmlmw_zlydth.png", CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, Name = "Mohamed Hassan", Email = "mohamed@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png", CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
        };

        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Development", Description = "Software development tasks", Color = "#0d6efd", UserId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Name = "Design", Description = "UI/UX design tasks", Color = "#6f42c1", UserId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, Name = "Marketing", Description = "Marketing and promotion tasks", Color = "#198754", UserId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 4, Name = "Meeting", Description = "Meetings and coordination", Color = "#fd7e14", UserId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 5, Name = "Research", Description = "Research and analysis tasks", Color = "#dc3545", UserId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
        };

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Set up project structure", Description = "Initialize the ASP.NET Core MVC project with proper folder structure", Status = Models.TaskItemStatus.Done, Priority = TaskPriority.High, CategoryId = 1, UserId = 1, CreatedAt = new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Title = "Design database schema", Description = "Create EF Core models and configure relationships", Status = Models.TaskItemStatus.Done, Priority = TaskPriority.High, CategoryId = 1, UserId = 1, CreatedAt = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, Title = "Create dashboard UI", Description = "Build an attractive dashboard with task statistics", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.High, CategoryId = 2, UserId = 2, CreatedAt = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 24, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 4, Title = "Implement task CRUD", Description = "Full CRUD operations for task management", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.Urgent, CategoryId = 1, UserId = 1, CreatedAt = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 23, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 5, Title = "Write unit tests", Description = "Cover controllers and services with unit tests", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Medium, CategoryId = 1, UserId = 3, CreatedAt = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 6, Title = "Marketing campaign Q3", Description = "Plan and execute Q3 marketing campaign", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Medium, CategoryId = 3, UserId = 2, CreatedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 7, Title = "Sprint planning meeting", Description = "Plan next sprint with the team", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Low, CategoryId = 4, UserId = 1, CreatedAt = new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 8, Title = "Competitor analysis", Description = "Research competitors and compile report", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.Medium, CategoryId = 5, UserId = 3, CreatedAt = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc) },
        };

        modelBuilder.Entity<User>().HasData(users);
        modelBuilder.Entity<Category>().HasData(categories);
        modelBuilder.Entity<TaskItem>().HasData(tasks);
    }
}
