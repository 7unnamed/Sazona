using BuildingBlocks.Domain.Auditing;
using Meals.Domain.Enums;

namespace Meals.Domain;

public class Plato : IAuditableEntity, ISoftDeletable
{
    public int IdPlato { get; set; }
    public string NombrePlato { get; set; } = string.Empty;
    public TipoComida TipoComida { get; set; }
    public int PorcionesBase { get; set; }
    public string? ImagenUrl { get; set; }

    public ICollection<PlatoIngrediente> PlatoIngredientes { get; set; } = new List<PlatoIngrediente>();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
