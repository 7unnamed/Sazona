using Planner.Api.Domain.Enums;

namespace Planner.Api.Domain;

public class HistorialEntry
{
    public int IdHistorialEntry { get; set; }
    public int IdPlato { get; set; }
    public DateOnly Fecha { get; set; }
    public TipoComida TipoComida { get; set; }
    public bool Confirmado { get; set; }
}
