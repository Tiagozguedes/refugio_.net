using Microsoft.EntityFrameworkCore;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

internal sealed class UsuarioRepository(RefugioDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailEmUsoAsync(string email, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.Email == email, ct);

    public async Task BloquearParaReservaAsync(long id, CancellationToken ct)
    {
        // SELECT ... FOR UPDATE na linha do idoso: duas reservas dele ao mesmo tempo (mesmo em refúgios
        // diferentes) passam a ser atendidas em fila, e a regra "uma por dia" não é furada.
        // Só o nome da tabela (vindo do modelo do EF) entra no texto; o id vai como parâmetro.
        var sql = $"SELECT * FROM {db.Tabela<Usuario>()} WHERE ID = {{0}} FOR UPDATE";
        await db.Usuarios.FromSqlRaw(sql, id).AsNoTracking().ToListAsync(ct);
    }

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
}
