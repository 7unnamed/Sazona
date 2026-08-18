using Meals.Application.Contracts;
using Meals.Application.Interfaces;
using Meals.Domain;

namespace Meals.Application.Services;

public class IngredienteService : IIngredienteService
{
    private readonly IIngredienteRepository _ingredienteRepository;

    public IngredienteService(IIngredienteRepository ingredienteRepository)
    {
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task<List<IngredienteResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var ingredientes = await _ingredienteRepository.GetAllAsync(cancellationToken);
        return ingredientes.Select(ToResponse).ToList();
    }

    public async Task<IngredienteResponse?> GetByIdAsync(int idIngrediente, CancellationToken cancellationToken = default)
    {
        var ingrediente = await _ingredienteRepository.GetByIdAsync(idIngrediente, cancellationToken);
        return ingrediente is null ? null : ToResponse(ingrediente);
    }

    public async Task<IngredienteResponse> CreateAsync(CrearIngredienteRequest request, CancellationToken cancellationToken = default)
    {
        var ingrediente = new Ingrediente
        {
            Nombre = request.Nombre,
            PaisProcedencia = request.PaisProcedencia,
            Categoria = request.Categoria,
            Descripcion = request.Descripcion
        };

        _ingredienteRepository.Add(ingrediente);
        await _ingredienteRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(ingrediente);
    }

    public async Task<bool> UpdateAsync(int idIngrediente, ActualizarIngredienteRequest request, CancellationToken cancellationToken = default)
    {
        var ingrediente = await _ingredienteRepository.GetByIdAsync(idIngrediente, cancellationToken);
        if (ingrediente is null)
        {
            return false;
        }

        ingrediente.Nombre = request.Nombre;
        ingrediente.PaisProcedencia = request.PaisProcedencia;
        ingrediente.Categoria = request.Categoria;
        ingrediente.Descripcion = request.Descripcion;

        await _ingredienteRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int idIngrediente, CancellationToken cancellationToken = default)
    {
        var ingrediente = await _ingredienteRepository.GetByIdAsync(idIngrediente, cancellationToken);
        if (ingrediente is null)
        {
            return false;
        }

        _ingredienteRepository.Remove(ingrediente);
        await _ingredienteRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IngredienteResponse ToResponse(Ingrediente ingrediente) => new(
        ingrediente.IdIngrediente,
        ingrediente.Nombre,
        ingrediente.PaisProcedencia,
        ingrediente.Categoria,
        ingrediente.Descripcion);
}
