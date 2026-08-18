using BuildingBlocks.Domain.Auditing;

namespace Meals.Domain;

public class PlatoIngrediente : IAuditableEntity, ISoftDeletable
{
    public int IdPlatoIngrediente { get; set; }

    public int IdPlato { get; set; }
    public Plato Plato { get; set; } = null!;

    public int IdIngrediente { get; set; }
    public Ingrediente Ingrediente { get; set; } = null!;

    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
