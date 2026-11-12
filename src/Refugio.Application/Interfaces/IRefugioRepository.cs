namespace Refugio.Application.Interfaces;

using Refugio.Domain.Entities;

public interface IRefugioRepository
{
    Task<Refugio?> ObterPorIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// Lê o refúgio bloqueando a linha até o fim da transação. Duas reservas simultâneas
    /// no mesmo refúgio passam a ser atendidas em fila, e a contagem de vagas não estoura.
    /// </summary>
    Task<Refugio?> ObterParaReservaAsync(long id, CancellationToken ct);

    Task<(IReadOnlyList<RefugioComOcupacao> Itens, int Total)> ListarAsync(
        string? bairro, DateOnly data, int pagina, int tamanho, CancellationToken ct);

    void Adicionar(Refugio refugio);

    void Remover(Refugio refugio);
}

/// <summary>Refúgio com o número de reservas ativas na data consultada.</summary>
public sealed record RefugioComOcupacao(Refugio Refugio, int VagasOcupadas);
