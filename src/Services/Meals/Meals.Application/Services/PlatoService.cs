using Meals.Application.Contracts;
using Meals.Application.Interfaces;
using Meals.Domain;

namespace Meals.Application.Services;

public class PlatoService : IPlatoService
{
    private readonly IPlatoRepository _platoRepository;

    public PlatoService(IPlatoRepository platoRepository)
    {
        _platoRepository = platoRepository;
    }

    public async Task<List<PlatoResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var platos = await _platoRepository.GetAllAsync(cancellationToken);
        return platos.Select(ToResponse).ToList();
    }

    public async Task<PlatoResponse?> GetByIdAsync(int idPlato, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        return plato is null ? null : ToResponse(plato);
    }

    public async Task<PlatoResponse> CreateAsync(CrearPlatoRequest request, CancellationToken cancellationToken = default)
    {
        var plato = new Plato
        {
            NombrePlato = request.NombrePlato,
            TipoComida = request.TipoComida,
            PorcionesBase = request.PorcionesBase,
            Ingredientes = request.Ingredientes.Select(i => new Ingrediente
            {
                NombreIngrediente = i.NombreIngrediente,
                Cantidad = i.Cantidad,
                Unidad = i.Unidad
            }).ToList()
        };

        _platoRepository.Add(plato);
        await _platoRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(plato);
    }

    public async Task<bool> UpdateAsync(int idPlato, ActualizarPlatoRequest request, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        if (plato is null)
        {
            return false;
        }

        plato.NombrePlato = request.NombrePlato;
        plato.TipoComida = request.TipoComida;
        plato.PorcionesBase = request.PorcionesBase;

        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int idPlato, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        if (plato is null)
        {
            return false;
        }

        _platoRepository.Remove(plato);
        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IngredienteResponse?> AddIngredienteAsync(int idPlato, IngredienteRequest request, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        if (plato is null)
        {
            return null;
        }

        var ingrediente = new Ingrediente
        {
            NombreIngrediente = request.NombreIngrediente,
            Cantidad = request.Cantidad,
            Unidad = request.Unidad
        };

        plato.Ingredientes.Add(ingrediente);
        await _platoRepository.SaveChangesAsync(cancellationToken);

        return new IngredienteResponse(ingrediente.IdIngrediente, ingrediente.NombreIngrediente, ingrediente.Cantidad, ingrediente.Unidad);
    }

    public async Task<bool> UpdateIngredienteAsync(int idPlato, int idIngrediente, IngredienteRequest request, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        var ingrediente = plato?.Ingredientes.FirstOrDefault(i => i.IdIngrediente == idIngrediente);
        if (ingrediente is null)
        {
            return false;
        }

        ingrediente.NombreIngrediente = request.NombreIngrediente;
        ingrediente.Cantidad = request.Cantidad;
        ingrediente.Unidad = request.Unidad;

        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveIngredienteAsync(int idPlato, int idIngrediente, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        var ingrediente = plato?.Ingredientes.FirstOrDefault(i => i.IdIngrediente == idIngrediente);
        if (ingrediente is null)
        {
            return false;
        }

        plato!.Ingredientes.Remove(ingrediente);
        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PlatoResponse ToResponse(Plato plato) => new(
        plato.IdPlato,
        plato.NombrePlato,
        plato.TipoComida,
        plato.PorcionesBase,
        plato.Ingredientes.Select(i => new IngredienteResponse(
            i.IdIngrediente,
            i.NombreIngrediente,
            i.Cantidad,
            i.Unidad)).ToList());
}
