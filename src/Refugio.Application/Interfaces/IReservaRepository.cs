using Refugio.Domain.Entities;

namespace Refugio.Application.Interfaces;

public interface IReservaRepository
{
    /// <summary>Inclui o refúgio, para montar nome e endereço na resposta.</summary>
    Task<Reserva?> ObterPorIdAsync(long id, CancellationToken ct);

    Task<Reserva?> ObterPorChaveIdempotenciaAsync(long idosoId, string chave, CancellationToken ct);

    /// <summary>Reservas ativas do idoso a partir da data, da mais próxima para a mais distante.</summary>
    Task<IReadOnlyList<Reserva>> ListarAtivasDoIdosoAPartirDeAsync(long idosoId, DateOnly data, CancellationToken ct);

    Task<bool> IdosoTemReservaAtivaNoDiaAsync(long idosoId, DateOnly data, CancellationToken ct);

    Task<int> ContarAtivasAsync(long refugioId, DateOnly data, CancellationToken ct);

    Task<bool> RefugioTemReservasAtivasAPartirDeAsync(long refugioId, DateOnly data, CancellationToken ct);

    void Adicionar(Reserva reserva);
}
