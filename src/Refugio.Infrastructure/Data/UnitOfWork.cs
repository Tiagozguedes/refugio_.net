using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Refugio.Application.Interfaces;

namespace Refugio.Infrastructure.Data;

internal sealed class UnitOfWork(RefugioDbContext db) : IUnitOfWork
{
    public async Task SalvarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new RegistroDuplicadoException(ex);
        }
    }

    public async Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct) =>
        new Transacao(await db.Database.BeginTransactionAsync(ct));

    private sealed class Transacao(IDbContextTransaction transacao) : ITransacao
    {
        public Task ConfirmarAsync(CancellationToken ct) => transacao.CommitAsync(ct);

        public ValueTask DisposeAsync() => transacao.DisposeAsync();
    }
}
