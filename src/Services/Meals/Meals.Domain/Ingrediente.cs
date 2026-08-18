using BuildingBlocks.Domain.Auditing;

namespace Meals.Domain;

public class Ingrediente : IAuditableEntity, ISoftDeletable
{
    public int IdIngrediente { get; set; }
    public string NombreIngrediente { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;

    public int IdPlato { get; set; }
    public Plato Plato { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
