using Microsoft.EntityFrameworkCore;
using TMS.Models;

namespace TMS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();
    public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskActivityLog> TaskActivityLogs => Set<TaskActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasIndex(o => o.Name);
        });

        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.ToTable("OrganizationMemberships");
            entity.HasIndex(om => new { om.OrganizationId, om.UserId }).IsUnique();
            entity.HasOne(om => om.Organization)
                  .WithMany(o => o.Memberships)
                  .HasForeignKey(om => om.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(om => om.User)
                  .WithMany(u => u.OrganizationMemberships)
                  .HasForeignKey(om => om.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams");
            entity.HasOne(t => t.Organization)
                  .WithMany()
                  .HasForeignKey(t => t.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.Manager)
                  .WithMany()
                  .HasForeignKey(t => t.ManagerUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TeamMembership>(entity =>
        {
            entity.ToTable("TeamMemberships");
            entity.HasIndex(tm => new { tm.TeamId, tm.UserId }).IsUnique();
            entity.HasOne(tm => tm.Team)
                  .WithMany(t => t.Memberships)
                  .HasForeignKey(tm => tm.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tm => tm.User)
                  .WithMany(u => u.TeamMemberships)
                  .HasForeignKey(tm => tm.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskAssignee>(entity =>
        {
            entity.ToTable("TaskAssignees");
            entity.HasIndex(ta => new { ta.TaskItemId, ta.UserId }).IsUnique();
            entity.HasOne(ta => ta.TaskItem)
                  .WithMany(t => t.Assignees)
                  .HasForeignKey(ta => ta.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ta => ta.User)
                  .WithMany(u => u.TaskAssignments)
                  .HasForeignKey(ta => ta.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasOne(p => p.Organization)
                  .WithMany()
                  .HasForeignKey(p => p.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Board>(entity =>
        {
            entity.ToTable("Boards");
            entity.HasOne(b => b.Project)
                  .WithMany(p => p.Boards)
                  .HasForeignKey(b => b.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BoardColumn>(entity =>
        {
            entity.ToTable("BoardColumns");
            entity.HasOne(bc => bc.Board)
                  .WithMany(b => b.Columns)
                  .HasForeignKey(bc => bc.BoardId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(bc => new { bc.BoardId, bc.Order });
        });

        modelBuilder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("TaskComments");
            entity.HasOne(tc => tc.TaskItem)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(tc => tc.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tc => tc.User)
                  .WithMany(u => u.TaskComments)
                  .HasForeignKey(tc => tc.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(tc => tc.ParentComment)
                  .WithMany(tc => tc.Replies)
                  .HasForeignKey(tc => tc.ParentCommentId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(tc => tc.TaskItemId);
            entity.HasIndex(tc => tc.ParentCommentId);
        });

        modelBuilder.Entity<TaskActivityLog>(entity =>
        {
            entity.ToTable("TaskActivityLogs");
            entity.HasOne(al => al.TaskItem)
                  .WithMany(t => t.ActivityLogs)
                  .HasForeignKey(al => al.TaskItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(al => al.User)
                  .WithMany(u => u.ActivityLogs)
                  .HasForeignKey(al => al.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(al => al.TaskItemId);
            entity.HasIndex(al => al.CreatedAt);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.IsRead);
            entity.HasIndex(n => n.CreatedAt);
        });

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
            entity.HasOne(t => t.Organization)
                  .WithMany()
                  .HasForeignKey(t => t.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.BoardColumn)
                  .WithMany(bc => bc.Tasks)
                  .HasForeignKey(t => t.BoardColumnId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);
            entity.HasIndex(t => t.DueDate);
            entity.HasIndex(t => t.CategoryId);
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => t.OrganizationId);
            entity.HasIndex(t => t.BoardColumnId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasIndex(c => new { c.Name, c.OrganizationId }).IsUnique();
            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Organization)
                  .WithMany()
                  .HasForeignKey(c => c.OrganizationId)
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

        var org = new Organization
        {
            Id = 1,
            Name = "TaskFlow Demo",
            CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc)
        };

        var users = new List<User>
        {
            new() { Id = 1, Name = "Ahmed Hamada", Email = "ahmed@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png", CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Name = "Sara Ali", Email = "sara@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Female/Number_47_ssmlmw_zlydth.png", CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, Name = "Mohamed Hassan", Email = "mohamed@tms.com", Password = pwd, AvatarUrl = "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png", CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
        };

        var memberships = new List<OrganizationMembership>
        {
            new() { Id = 1, OrganizationId = 1, UserId = 1, Role = OrganizationRole.Admin, JoinedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, OrganizationId = 1, UserId = 2, Role = OrganizationRole.Employee, JoinedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, OrganizationId = 1, UserId = 3, Role = OrganizationRole.Employee, JoinedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
        };

        var team = new Team
        {
            Id = 1,
            Name = "Engineering",
            OrganizationId = 1,
            ManagerUserId = 1,
            CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc)
        };

        var teamMemberships = new List<TeamMembership>
        {
            new() { Id = 1, TeamId = 1, UserId = 1, Role = TeamRole.Lead, JoinedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, TeamId = 1, UserId = 2, Role = TeamRole.Member, JoinedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, TeamId = 1, UserId = 3, Role = TeamRole.Member, JoinedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
        };

        var project = new Project
        {
            Id = 1,
            Name = "Software Development",
            OrganizationId = 1,
            CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc)
        };

        var board = new Board
        {
            Id = 1,
            Name = "Sprint Board",
            ProjectId = 1,
            CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc)
        };

        var boardColumns = new List<BoardColumn>
        {
            new() { Id = 1, BoardId = 1, Name = "To Do", Order = 0, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, BoardId = 1, Name = "In Progress", Order = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, BoardId = 1, Name = "Review", Order = 2, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 4, BoardId = 1, Name = "Done", Order = 3, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
        };

        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Development", Description = "Software development tasks", Color = "#0d6efd", UserId = 1, OrganizationId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Name = "Design", Description = "UI/UX design tasks", Color = "#6f42c1", UserId = 1, OrganizationId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, Name = "Marketing", Description = "Marketing and promotion tasks", Color = "#198754", UserId = 1, OrganizationId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 4, Name = "Meeting", Description = "Meetings and coordination", Color = "#fd7e14", UserId = 1, OrganizationId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 5, Name = "Research", Description = "Research and analysis tasks", Color = "#dc3545", UserId = 1, OrganizationId = 1, CreatedAt = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc) },
        };

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Set up project structure", Description = "Initialize the ASP.NET Core MVC project with proper folder structure", Status = Models.TaskItemStatus.Done, Priority = TaskPriority.High, CategoryId = 1, UserId = 1, OrganizationId = 1, BoardColumnId = 4, BoardOrder = 0, EstimatedHours = 4, CreatedAt = new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Title = "Design database schema", Description = "Create EF Core models and configure relationships", Status = Models.TaskItemStatus.Done, Priority = TaskPriority.High, CategoryId = 1, UserId = 1, OrganizationId = 1, BoardColumnId = 4, BoardOrder = 1, EstimatedHours = 6, CreatedAt = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, Title = "Create dashboard UI", Description = "Build an attractive dashboard with task statistics", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.High, CategoryId = 2, UserId = 2, OrganizationId = 1, BoardColumnId = 2, BoardOrder = 0, EstimatedHours = 12, CreatedAt = new DateTime(2026, 7, 17, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 24, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 4, Title = "Implement task CRUD", Description = "Full CRUD operations for task management", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.Urgent, CategoryId = 1, UserId = 1, OrganizationId = 1, BoardColumnId = 2, BoardOrder = 1, EstimatedHours = 8, CreatedAt = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 23, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 5, Title = "Write unit tests", Description = "Cover controllers and services with unit tests", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Medium, CategoryId = 1, UserId = 3, OrganizationId = 1, BoardColumnId = 1, BoardOrder = 0, EstimatedHours = 8, CreatedAt = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 6, Title = "Marketing campaign Q3", Description = "Plan and execute Q3 marketing campaign", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Medium, CategoryId = 3, UserId = 2, OrganizationId = 1, BoardColumnId = 1, BoardOrder = 1, EstimatedHours = 16, CreatedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 7, Title = "Sprint planning meeting", Description = "Plan next sprint with the team", Status = Models.TaskItemStatus.ToDo, Priority = TaskPriority.Low, CategoryId = 4, UserId = 1, OrganizationId = 1, BoardColumnId = 1, BoardOrder = 2, EstimatedHours = 2, CreatedAt = new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 8, Title = "Competitor analysis", Description = "Research competitors and compile report", Status = Models.TaskItemStatus.InProgress, Priority = TaskPriority.Medium, CategoryId = 5, UserId = 3, OrganizationId = 1, BoardColumnId = 2, BoardOrder = 2, EstimatedHours = 6, CreatedAt = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), DueDate = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc) },
        };

        var taskAssignees = new List<TaskAssignee>
        {
            new() { Id = 1, TaskItemId = 1, UserId = 1, IsPrimary = true },
            new() { Id = 2, TaskItemId = 2, UserId = 1, IsPrimary = true },
            new() { Id = 3, TaskItemId = 3, UserId = 2, IsPrimary = true },
            new() { Id = 4, TaskItemId = 4, UserId = 1, IsPrimary = true },
            new() { Id = 5, TaskItemId = 5, UserId = 3, IsPrimary = true },
            new() { Id = 6, TaskItemId = 6, UserId = 2, IsPrimary = true },
            new() { Id = 7, TaskItemId = 7, UserId = 1, IsPrimary = true },
            new() { Id = 8, TaskItemId = 8, UserId = 3, IsPrimary = true },
        };

        modelBuilder.Entity<Organization>().HasData(org);
        modelBuilder.Entity<User>().HasData(users);
        modelBuilder.Entity<OrganizationMembership>().HasData(memberships);
        modelBuilder.Entity<Team>().HasData(team);
        modelBuilder.Entity<TeamMembership>().HasData(teamMemberships);
        modelBuilder.Entity<Project>().HasData(project);
        modelBuilder.Entity<Board>().HasData(board);
        modelBuilder.Entity<BoardColumn>().HasData(boardColumns);
        modelBuilder.Entity<Category>().HasData(categories);
        modelBuilder.Entity<TaskItem>().HasData(tasks);
        modelBuilder.Entity<TaskAssignee>().HasData(taskAssignees);
    }
}
