using Microsoft.EntityFrameworkCore;

namespace Refugio.Infrastructure.Data;

public class RefugioDbContext(DbContextOptions<RefugioDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applies every IEntityTypeConfiguration<T> declared in Mappings/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RefugioDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
