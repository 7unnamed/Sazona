using BuildingBlocks.Domain.Auditing;
using Meals.Domain.Enums;

namespace Meals.Domain;

public class Ingrediente : IAuditableEntity, ISoftDeletable
{
    public int IdIngrediente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PaisProcedencia { get; set; } = string.Empty;
    public CategoriaIngrediente Categoria { get; set; }
    public string? Descripcion { get; set; }

    public ICollection<PlatoIngrediente> PlatoIngredientes { get; set; } = new List<PlatoIngrediente>();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
