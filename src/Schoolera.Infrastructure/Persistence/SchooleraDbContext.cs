using Microsoft.EntityFrameworkCore;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence;

public sealed class SchooleraDbContext(DbContextOptions<SchooleraDbContext> options)
    : DbContext(options)
{
    public DbSet<School> Schools => Set<School>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchooleraDbContext).Assembly);
    }
}