using Planner.Domain.Enums;

namespace Planner.Application.Contracts;

public record CrearHistorialEntryRequest(int IdPlato, DateOnly Fecha, TipoComida TipoComida, bool Confirmado = false);

public record ActualizarHistorialEntryRequest(int IdPlato, DateOnly Fecha, TipoComida TipoComida, bool Confirmado);

public record HistorialEntryResponse(int IdHistorialEntry, int IdPlato, DateOnly Fecha, TipoComida TipoComida, bool Confirmado);
