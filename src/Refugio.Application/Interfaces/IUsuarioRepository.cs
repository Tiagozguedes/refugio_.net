using Refugio.Domain.Entities;

namespace Refugio.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct);

    /// <summary>Recebe o e-mail já normalizado (minúsculas, sem espaços).</summary>
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct);

    Task<bool> EmailEmUsoAsync(string email, CancellationToken ct);

    /// <summary>Bloqueia a linha do idoso até o fim da transação, para serializar as reservas dele.</summary>
    Task BloquearParaReservaAsync(long id, CancellationToken ct);

    void Adicionar(Usuario usuario);
}
