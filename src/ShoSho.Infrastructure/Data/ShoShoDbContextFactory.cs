using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShoSho.Infrastructure.Data;

public class ShoShoDbContextFactory : IDesignTimeDbContextFactory<ShoShoDbContext>
{
    public ShoShoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShoShoDbContext>();
        ShoShoDbContext.Configure(optionsBuilder, ShoShoDbContext.DefaultConnectionString);
        return new ShoShoDbContext(optionsBuilder.Options);
    }
}

