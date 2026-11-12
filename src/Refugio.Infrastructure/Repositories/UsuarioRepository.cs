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

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
}
