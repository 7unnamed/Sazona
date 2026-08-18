using BuildingBlocks.Domain.Auditing;
using Planner.Domain.Enums;

namespace Planner.Domain;

public class HistorialEntry : IAuditableEntity, ISoftDeletable
{
    public int IdHistorialEntry { get; set; }
    public int IdPlato { get; set; }
    public DateOnly Fecha { get; set; }
    public TipoComida TipoComida { get; set; }
    public bool Confirmado { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
