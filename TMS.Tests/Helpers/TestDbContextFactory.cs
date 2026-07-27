using Microsoft.EntityFrameworkCore;
using TMS.Data;

namespace TMS.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }
}
