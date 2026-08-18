using Meals.Api.Domain.Enums;

namespace Meals.Api.Domain;

public class Plato
{
    public int IdPlato { get; set; }
    public string NombrePlato { get; set; } = string.Empty;
    public TipoComida TipoComida { get; set; }
    public int PorcionesBase { get; set; }

    public ICollection<Ingrediente> Ingredientes { get; set; } = new List<Ingrediente>();
}
