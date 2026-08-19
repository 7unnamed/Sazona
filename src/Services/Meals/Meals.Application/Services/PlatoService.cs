using Meals.Application.Contracts;
using Meals.Application.Interfaces;
using Meals.Domain;

namespace Meals.Application.Services;

public class PlatoService : IPlatoService
{
    private readonly IPlatoRepository _platoRepository;
    private readonly IIngredienteRepository _ingredienteRepository;

    public PlatoService(IPlatoRepository platoRepository, IIngredienteRepository ingredienteRepository)
    {
        _platoRepository = platoRepository;
        _ingredienteRepository = ingredienteRepository;
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

    public async Task<List<PlatoResponse>> SearchByNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var platos = await _platoRepository.SearchByNombreAsync(nombre, cancellationToken);
        return platos.Select(ToResponse).ToList();
    }

    public async Task<PlatoResponse?> GetRandomAsync(CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetRandomAsync(cancellationToken);
        return plato is null ? null : ToResponse(plato);
    }

    public async Task<List<PlatoResponse>> GetNoCocinadosAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        var platos = await _platoRepository.GetNoCocinadosAsync(idUsuario, cancellationToken);
        return platos.Select(ToResponse).ToList();
    }

    public async Task<PlatoResponse> CreateAsync(CrearPlatoRequest request, CancellationToken cancellationToken = default)
    {
        var plato = new Plato
        {
            NombrePlato = request.NombrePlato,
            TipoComida = request.TipoComida,
            PorcionesBase = request.PorcionesBase
        };

        foreach (var item in request.Ingredientes)
        {
            var ingrediente = await _ingredienteRepository.GetByIdAsync(item.IdIngrediente, cancellationToken)
                ?? throw new InvalidOperationException($"No existe ningún ingrediente en el catálogo con Id {item.IdIngrediente}.");

            plato.PlatoIngredientes.Add(new PlatoIngrediente
            {
                IdIngrediente = ingrediente.IdIngrediente,
                Ingrediente = ingrediente,
                Cantidad = item.Cantidad,
                Unidad = item.Unidad
            });
        }

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

    public async Task<PlatoResponse?> SetImagenAsync(int idPlato, string imagenUrl, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        if (plato is null)
        {
            return null;
        }

        plato.ImagenUrl = imagenUrl;
        await _platoRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(plato);
    }

    public async Task<PlatoIngredienteResponse?> AddIngredienteAsync(int idPlato, AgregarIngredienteAPlatoRequest request, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        if (plato is null)
        {
            return null;
        }

        var ingrediente = await _ingredienteRepository.GetByIdAsync(request.IdIngrediente, cancellationToken);
        if (ingrediente is null)
        {
            throw new InvalidOperationException($"No existe ningún ingrediente en el catálogo con Id {request.IdIngrediente}.");
        }

        var platoIngrediente = new PlatoIngrediente
        {
            IdIngrediente = ingrediente.IdIngrediente,
            Ingrediente = ingrediente,
            Cantidad = request.Cantidad,
            Unidad = request.Unidad
        };

        plato.PlatoIngredientes.Add(platoIngrediente);
        await _platoRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(platoIngrediente);
    }

    public async Task<bool> UpdateIngredienteAsync(int idPlato, int idPlatoIngrediente, ActualizarPlatoIngredienteRequest request, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        var platoIngrediente = plato?.PlatoIngredientes.FirstOrDefault(pi => pi.IdPlatoIngrediente == idPlatoIngrediente);
        if (platoIngrediente is null)
        {
            return false;
        }

        platoIngrediente.Cantidad = request.Cantidad;
        platoIngrediente.Unidad = request.Unidad;

        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveIngredienteAsync(int idPlato, int idPlatoIngrediente, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        var platoIngrediente = plato?.PlatoIngredientes.FirstOrDefault(pi => pi.IdPlatoIngrediente == idPlatoIngrediente);
        if (platoIngrediente is null)
        {
            return false;
        }

        plato!.PlatoIngredientes.Remove(platoIngrediente);
        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PasoPreparacionResponse?> AddPasoAsync(int idPlato, AgregarPasoPreparacionRequest request, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        if (plato is null)
        {
            return null;
        }

        var siguienteOrden = plato.PasosPreparacion.Count == 0 ? 1 : plato.PasosPreparacion.Max(p => p.Orden) + 1;
        var paso = new PasoPreparacion
        {
            IdPlato = idPlato,
            Orden = siguienteOrden,
            Descripcion = request.Descripcion
        };

        plato.PasosPreparacion.Add(paso);
        await _platoRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(paso);
    }

    public async Task<bool> UpdatePasoAsync(int idPlato, int idPaso, ActualizarPasoPreparacionRequest request, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        var paso = plato?.PasosPreparacion.FirstOrDefault(p => p.IdPasoPreparacion == idPaso);
        if (paso is null)
        {
            return false;
        }

        paso.Descripcion = request.Descripcion;
        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemovePasoAsync(int idPlato, int idPaso, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        var paso = plato?.PasosPreparacion.FirstOrDefault(p => p.IdPasoPreparacion == idPaso);
        if (paso is null)
        {
            return false;
        }

        plato!.PasosPreparacion.Remove(paso);
        await _platoRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarcarCocinadoAsync(int idPlato, int idUsuario, CancellationToken cancellationToken = default)
    {
        var plato = await _platoRepository.GetByIdAsync(idPlato, cancellationToken);
        if (plato is null)
        {
            return false;
        }

        await _platoRepository.MarcarCocinadoAsync(idPlato, idUsuario, cancellationToken);
        return true;
    }

    private static PlatoResponse ToResponse(Plato plato) => new(
        plato.IdPlato,
        plato.NombrePlato,
        plato.TipoComida,
        plato.PorcionesBase,
        plato.PlatoIngredientes.Select(ToResponse).ToList(),
        plato.ImagenUrl,
        plato.PasosPreparacion.OrderBy(p => p.Orden).Select(ToResponse).ToList());

    private static PlatoIngredienteResponse ToResponse(PlatoIngrediente platoIngrediente) => new(
        platoIngrediente.IdPlatoIngrediente,
        platoIngrediente.IdIngrediente,
        platoIngrediente.Ingrediente.Nombre,
        platoIngrediente.Cantidad,
        platoIngrediente.Unidad);

    private static PasoPreparacionResponse ToResponse(PasoPreparacion paso) => new(
        paso.IdPasoPreparacion,
        paso.Orden,
        paso.Descripcion);
}
