using BuildingBlocks.Application.Interfaces;
using BuildingBlocks.Infrastructure.Auditing;
using Planner.Domain;
using Microsoft.EntityFrameworkCore;

namespace Planner.Infrastructure.Data;

public class PlannerDbContext : AuditableDbContext
{
    public PlannerDbContext(DbContextOptions<PlannerDbContext> options, ICurrentUserService currentUserService)
        : base(options, currentUserService)
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

        ApplySoftDeleteQueryFilters(modelBuilder);
    }
}
