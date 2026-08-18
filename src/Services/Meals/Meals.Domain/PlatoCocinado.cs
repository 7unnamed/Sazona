using BuildingBlocks.Domain.Auditing;

namespace Meals.Domain;

public class PlatoCocinado : IAuditableEntity, ISoftDeletable
{
    public int IdPlatoCocinado { get; set; }
    public int IdUsuario { get; set; }
    public int IdPlato { get; set; }
    public Plato Plato { get; set; } = null!;
    public DateTime FechaCocinado { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
