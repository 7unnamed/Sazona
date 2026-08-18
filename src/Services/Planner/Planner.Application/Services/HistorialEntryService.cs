using Planner.Application.Contracts;
using Planner.Application.Interfaces;
using Planner.Domain;

namespace Planner.Application.Services;

public class HistorialEntryService : IHistorialEntryService
{
    private readonly IHistorialEntryRepository _historialEntryRepository;

    public HistorialEntryService(IHistorialEntryRepository historialEntryRepository)
    {
        _historialEntryRepository = historialEntryRepository;
    }

    public async Task<List<HistorialEntryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _historialEntryRepository.GetAllAsync(cancellationToken);
        return entries.Select(ToResponse).ToList();
    }

    public async Task<HistorialEntryResponse?> GetByIdAsync(int idHistorialEntry, CancellationToken cancellationToken = default)
    {
        var entry = await _historialEntryRepository.GetByIdAsync(idHistorialEntry, cancellationToken);
        return entry is null ? null : ToResponse(entry);
    }

    public async Task<HistorialEntryResponse> CreateAsync(CrearHistorialEntryRequest request, CancellationToken cancellationToken = default)
    {
        var entry = new HistorialEntry
        {
            IdPlato = request.IdPlato,
            Fecha = request.Fecha,
            TipoComida = request.TipoComida,
            Confirmado = request.Confirmado
        };

        _historialEntryRepository.Add(entry);
        await _historialEntryRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(entry);
    }

    public async Task<bool> UpdateAsync(int idHistorialEntry, ActualizarHistorialEntryRequest request, CancellationToken cancellationToken = default)
    {
        var entry = await _historialEntryRepository.GetByIdAsync(idHistorialEntry, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.IdPlato = request.IdPlato;
        entry.Fecha = request.Fecha;
        entry.TipoComida = request.TipoComida;
        entry.Confirmado = request.Confirmado;

        await _historialEntryRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int idHistorialEntry, CancellationToken cancellationToken = default)
    {
        var entry = await _historialEntryRepository.GetByIdAsync(idHistorialEntry, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        _historialEntryRepository.Remove(entry);
        await _historialEntryRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static HistorialEntryResponse ToResponse(HistorialEntry entry) => new(
        entry.IdHistorialEntry,
        entry.IdPlato,
        entry.Fecha,
        entry.TipoComida,
        entry.Confirmado);
}
