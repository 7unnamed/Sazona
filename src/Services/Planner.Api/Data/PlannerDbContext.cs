using Planner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Planner.Api.Data;

public class PlannerDbContext : DbContext
{
    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options)
    {
    }

    public DbSet<HistorialEntry> HistorialEntries => Set<HistorialEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistorialEntry>(entity =>
        {
            entity.HasKey(h => h.IdHistorialEntry);
            entity.Property(h => h.Confirmado).HasDefaultValue(false);
        });
    }
}
