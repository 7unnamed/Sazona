using BuildingBlocks.Application.Interfaces;
using BuildingBlocks.Infrastructure.Auditing;
using Meals.Domain;
using Microsoft.EntityFrameworkCore;

namespace Meals.Infrastructure.Data;

public class MealsDbContext : AuditableDbContext
{
    public MealsDbContext(DbContextOptions<MealsDbContext> options, ICurrentUserService currentUserService)
        : base(options, currentUserService)
    {
    }

    public DbSet<Plato> Platos => Set<Plato>();
    public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();
    public DbSet<PlatoIngrediente> PlatoIngredientes => Set<PlatoIngrediente>();
    public DbSet<Favorito> Favoritos => Set<Favorito>();
    public DbSet<PlatoCocinado> PlatosCocinados => Set<PlatoCocinado>();
    public DbSet<PasoPreparacion> PasosPreparacion => Set<PasoPreparacion>();

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
            entity.Property(i => i.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(i => i.PaisProcedencia).IsRequired().HasMaxLength(100);
            entity.Property(i => i.Descripcion).HasMaxLength(500);
        });

        modelBuilder.Entity<PlatoIngrediente>(entity =>
        {
            entity.HasKey(pi => pi.IdPlatoIngrediente);
            entity.Property(pi => pi.Unidad).IsRequired().HasMaxLength(50);

            entity.HasOne(pi => pi.Plato)
                .WithMany(p => p.PlatoIngredientes)
                .HasForeignKey(pi => pi.IdPlato)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pi => pi.Ingrediente)
                .WithMany(i => i.PlatoIngredientes)
                .HasForeignKey(pi => pi.IdIngrediente)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Favorito>(entity =>
        {
            entity.HasKey(f => f.IdFavorito);
            entity.HasIndex(f => new { f.IdUsuario, f.IdPlato }).IsUnique();

            entity.HasOne(f => f.Plato)
                .WithMany()
                .HasForeignKey(f => f.IdPlato)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatoCocinado>(entity =>
        {
            entity.HasKey(pc => pc.IdPlatoCocinado);

            entity.HasOne(pc => pc.Plato)
                .WithMany()
                .HasForeignKey(pc => pc.IdPlato)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasoPreparacion>(entity =>
        {
            entity.HasKey(p => p.IdPasoPreparacion);
            entity.Property(p => p.Descripcion).IsRequired().HasMaxLength(500);

            entity.HasOne(p => p.Plato)
                .WithMany(p => p.PasosPreparacion)
                .HasForeignKey(p => p.IdPlato)
                .OnDelete(DeleteBehavior.Cascade);
        });

        ApplySoftDeleteQueryFilters(modelBuilder);
    }
}
