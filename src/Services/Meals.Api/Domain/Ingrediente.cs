namespace Meals.Api.Domain;

public class Ingrediente
{
    public int IdIngrediente { get; set; }
    public string NombreIngrediente { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;

    public int IdPlato { get; set; }
    public Plato Plato { get; set; } = null!;
}
