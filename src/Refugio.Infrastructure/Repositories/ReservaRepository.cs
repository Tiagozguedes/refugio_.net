using Microsoft.EntityFrameworkCore;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

internal sealed class ReservaRepository(RefugioDbContext db) : IReservaRepository
{
    public Task<Reserva?> ObterPorIdAsync(long id, CancellationToken ct) =>
        db.Reservas.Include(r => r.Refugio).FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Reserva?> ObterPorChaveIdempotenciaAsync(long idosoId, string chave, CancellationToken ct) =>
        db.Reservas.Include(r => r.Refugio)
            .FirstOrDefaultAsync(r => r.IdosoId == idosoId && r.ChaveIdempotencia == chave, ct);

    public async Task<IReadOnlyList<Reserva>> ListarAtivasDoIdosoAPartirDeAsync(long idosoId, DateOnly data, CancellationToken ct) =>
        await db.Reservas.AsNoTracking()
            .Include(r => r.Refugio)
            .Where(r => r.IdosoId == idosoId && r.Status == StatusReserva.Ativa && r.DataReserva >= data)
            .OrderBy(r => r.DataReserva).ThenBy(r => r.Id)
            .ToListAsync(ct);

    public Task<bool> IdosoTemReservaAtivaNoDiaAsync(long idosoId, DateOnly data, CancellationToken ct) =>
        db.Reservas.AnyAsync(r => r.IdosoId == idosoId && r.DataReserva == data && r.Status == StatusReserva.Ativa, ct);

    public Task<int> ContarAtivasAsync(long refugioId, DateOnly data, CancellationToken ct) =>
        db.Reservas.CountAsync(r => r.RefugioId == refugioId && r.DataReserva == data && r.Status == StatusReserva.Ativa, ct);

    public Task<bool> RefugioTemReservasAtivasAPartirDeAsync(long refugioId, DateOnly data, CancellationToken ct) =>
        db.Reservas.AnyAsync(r => r.RefugioId == refugioId && r.DataReserva >= data && r.Status == StatusReserva.Ativa, ct);

    public void Adicionar(Reserva reserva) => db.Reservas.Add(reserva);
}
