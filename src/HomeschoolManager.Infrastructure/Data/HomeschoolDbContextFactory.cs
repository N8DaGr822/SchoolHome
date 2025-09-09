using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeschoolManager.Infrastructure.Data;

public class HomeschoolDbContextFactory : IDesignTimeDbContextFactory<HomeschoolDbContext>
{
    public HomeschoolDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HomeschoolDbContext>();
        optionsBuilder.UseSqlite("Data Source=homeschool.db");

        return new HomeschoolDbContext(optionsBuilder.Options);
    }
}
