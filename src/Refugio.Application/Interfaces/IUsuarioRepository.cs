using Refugio.Domain.Entities;

namespace Refugio.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct);

    /// <summary>Recebe o e-mail já normalizado (minúsculas, sem espaços).</summary>
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct);

    Task<bool> EmailEmUsoAsync(string email, CancellationToken ct);

    void Adicionar(Usuario usuario);
}
