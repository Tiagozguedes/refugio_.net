namespace Refugio.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Infrastructure.Data;

internal sealed class RefugioRepository(RefugioDbContext db) : IRefugioRepository
{
    public Task<Refugio?> ObterPorIdAsync(long id, CancellationToken ct) =>
        db.Refugios.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Refugio?> ObterParaReservaAsync(long id, CancellationToken ct)
    {
        // SELECT ... FOR UPDATE: segura a linha até o commit da transação da reserva.
        // Só o nome da tabela (vindo do modelo do EF) entra no texto; o id vai como parâmetro.
        var sql = $"SELECT * FROM {db.Tabela<Refugio>()} WHERE ID = {{0}} FOR UPDATE";
        var encontrados = await db.Refugios.FromSqlRaw(sql, id).ToListAsync(ct);
        return encontrados.SingleOrDefault();
    }

    public async Task<(IReadOnlyList<RefugioComOcupacao> Itens, int Total)> ListarAsync(
        string? bairro, bool? ativo, DateOnly data, int pagina, int tamanho, CancellationToken ct)
    {
        var consulta = db.Refugios.AsNoTracking();

        if (ativo is { } somenteAtivos)
            consulta = consulta.Where(r => r.Ativo == somenteAtivos);

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            var filtro = bairro.Trim().ToLower();
            consulta = consulta.Where(r => r.Bairro.ToLower() == filtro);
        }

        var total = await consulta.CountAsync(ct);

        var itens = await consulta
            .OrderBy(r => r.Nome).ThenBy(r => r.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .Select(r => new
            {
                Refugio = r,
                Ocupadas = db.Reservas.Count(x =>
                    x.RefugioId == r.Id && x.DataReserva == data && x.Status == StatusReserva.Ativa),
            })
            .ToListAsync(ct);

        return (itens.Select(i => new RefugioComOcupacao(i.Refugio, i.Ocupadas)).ToList(), total);
    }

    public void Adicionar(Refugio refugio) => db.Refugios.Add(refugio);

    public void Remover(Refugio refugio) => db.Refugios.Remove(refugio);
}
