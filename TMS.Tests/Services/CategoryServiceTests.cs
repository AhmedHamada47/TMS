using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TMS.Data;
using TMS.Models;
using TMS.Services;
using TMS.Tests.Helpers;

namespace TMS.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly string _dbName;

    public CategoryServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        Seed();
    }

    private AppDbContext CreateContext() => TestDbContextFactory.Create(_dbName);

    private void Seed()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Organizations.Add(new Organization { Id = 1, Name = "Org1" });
        ctx.Organizations.Add(new Organization { Id = 2, Name = "Org2" });
        ctx.Users.Add(new User { Id = 1, Name = "A", Email = "a@a.com", Password = "pwd" });
        ctx.Categories.Add(new Category { Id = 1, Name = "Dev", Color = "#00f", UserId = 1, OrganizationId = 1 });
        ctx.Categories.Add(new Category { Id = 2, Name = "Design", Color = "#f00", UserId = 1, OrganizationId = 1 });
        ctx.Categories.Add(new Category { Id = 3, Name = "OtherOrg", Color = "#0f0", UserId = 1, OrganizationId = 2 });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsOnlyOrgCategories()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);

        var cats = await svc.GetCategoriesAsync(1);

        cats.Should().HaveCount(2);
        cats.Should().Contain(c => c.Name == "Dev");
        cats.Should().NotContain(c => c.Name == "OtherOrg");
    }

    [Fact]
    public async Task GetCategoryAsync_ReturnsNull_WhenDifferentOrg()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);

        var cat = await svc.GetCategoryAsync(1, 2);

        cat.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryAsync_ReturnsCategory_WhenSameOrg()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);

        var cat = await svc.GetCategoryAsync(1, 1);

        cat.Should().NotBeNull();
        cat!.Name.Should().Be("Dev");
    }

    [Fact]
    public async Task CreateCategoryAsync_CreatesWithCorrectOrg()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);
        var cat = new Category { Name = "NewCat", Color = "#abc", Description = "test" };

        await svc.CreateCategoryAsync(cat, 1, 1);

        var saved = await ctx.Categories.FirstOrDefaultAsync(c => c.Name == "NewCat");
        saved.Should().NotBeNull();
        saved!.OrganizationId.Should().Be(1);
        saved.UserId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateCategoryAsync_OnlyUpdatesWithinOrg()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);
        var updated = new Category { Name = "UpdatedDev", Color = "#111", Description = "desc" };

        await svc.UpdateCategoryAsync(1, updated, 1);

        var cat = await ctx.Categories.FindAsync(1);
        cat!.Name.Should().Be("UpdatedDev");
    }

    [Fact]
    public async Task DeleteCategoryAsync_DoesNotDelete_WhenDifferentOrg()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);

        await svc.DeleteCategoryAsync(3, 1);

        var cat = await ctx.Categories.FindAsync(3);
        cat.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCategoryAsync_DeletesWhenSameOrg()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);

        await svc.DeleteCategoryAsync(1, 1);

        var cat = await ctx.Categories.FindAsync(1);
        cat.Should().BeNull();
    }

    [Fact]
    public async Task GetSidebarCategoriesAsync_ReturnsMappedData()
    {
        using var ctx = CreateContext();
        var svc = new CategoryService(ctx);

        var cats = await svc.GetSidebarCategoriesAsync(1);

        cats.Should().HaveCount(2);
        cats.All(c => c.TaskCount >= 0).Should().BeTrue();
    }

    public void Dispose()
    {
        using var ctx = TestDbContextFactory.Create(_dbName);
        ctx.Database.EnsureDeleted();
    }
}
