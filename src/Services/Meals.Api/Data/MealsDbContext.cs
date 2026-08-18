using Meals.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Meals.Api.Data;

public class MealsDbContext : DbContext
{
    public MealsDbContext(DbContextOptions<MealsDbContext> options) : base(options)
    {
    }

    public DbSet<Plato> Platos => Set<Plato>();
    public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plato>(entity =>
        {
            entity.HasKey(p => p.IdPlato);
            entity.Property(p => p.NombrePlato).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Ingrediente>(entity =>
        {
            entity.HasKey(i => i.IdIngrediente);
            entity.Property(i => i.NombreIngrediente).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Unidad).IsRequired().HasMaxLength(50);

            entity.HasOne(i => i.Plato)
                .WithMany(p => p.Ingredientes)
                .HasForeignKey(i => i.IdPlato)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
