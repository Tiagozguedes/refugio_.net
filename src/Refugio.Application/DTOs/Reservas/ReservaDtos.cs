using Refugio.Application.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

namespace Refugio.Application.DTOs.Reservas;

/// <summary>Corpo de POST /api/v1/reservas. O idoso vem do token, nunca do corpo.</summary>
public sealed record CriarReservaRequest(long? RefugioId, DateOnly? DataReserva);

/// <summary>Mesmo formato no POST, em GET /reservas/minhas e em GET /reservas/{id}.</summary>
public sealed record ReservaResponse(
    long Id,
    long RefugioId,
    string RefugioNome,
    string RefugioEndereco,
    DateOnly DataReserva,
    StatusReserva Status,
    DateTimeOffset CriadoEm)
{
    public static ReservaResponse De(Reserva r) => new(
        r.Id, r.RefugioId, r.Refugio.Nome, r.Refugio.EnderecoCompleto,
        r.DataReserva, r.Status, FusoHorario.ParaSaoPaulo(r.CriadoEm));
}
