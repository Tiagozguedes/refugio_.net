namespace Refugio.Application.Interfaces;

public interface IUnitOfWork
{
    /// <summary>Grava as alterações. Violação de chave única vira <see cref="RegistroDuplicadoException"/>.</summary>
    Task SalvarAsync(CancellationToken ct);

    Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct);
}

/// <summary>Sem <see cref="ConfirmarAsync"/>, o descarte desfaz tudo.</summary>
public interface ITransacao : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken ct);
}

/// <summary>Lançada quando o banco recusa um registro por índice único (ex.: duas reservas do idoso no mesmo dia).</summary>
public sealed class RegistroDuplicadoException(Exception causa)
    : Exception("O registro viola uma restrição de unicidade.", causa);
