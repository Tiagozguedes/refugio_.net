using Refugio.Application.Common;
using Refugio.Application.DTOs.Reservas;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Domain.Exceptions;

namespace Refugio.Application.UseCases.Reservas;

internal static class ReservaErros
{
    public static RecursoNaoEncontradoException ReservaNaoEncontrada() =>
        new("Reserva não encontrada", "Esta reserva não existe.");

    public static RecursoNaoEncontradoException RefugioNaoEncontrado() =>
        new("Refúgio não encontrado", "Este refúgio não está mais disponível.");

    public static AcessoNegadoException DeOutroIdoso() =>
        new("Acesso negado", "Você não tem permissão para isso.");

    public static ConflitoException JaTemReservaNoDia(DateOnly data) =>
        new("Reserva já existe", $"Você já tem uma reserva para {data:dd/MM}.");

    /// <summary>Carrega a reserva e garante que ela é do idoso logado: 404 se não existe, 403 se é de outro.</summary>
    public static async Task<Reserva> ObterDoIdosoAsync(IReservaRepository reservas, long id, long idosoId, CancellationToken ct)
    {
        var reserva = await reservas.ObterPorIdAsync(id, ct) ?? throw ReservaNaoEncontrada();
        return reserva.PertenceA(idosoId) ? reserva : throw DeOutroIdoso();
    }
}

public sealed class CriarReserva(
    IRefugioRepository refugios,
    IReservaRepository reservas,
    IUnitOfWork unitOfWork,
    IUsuarioAtual usuarioAtual,
    TimeProvider relogio)
{
    public async Task<ReservaResponse> ExecutarAsync(CriarReservaRequest request, string chaveIdempotencia, CancellationToken ct)
    {
        var idosoId = usuarioAtual.Id;
        var refugioId = request.RefugioId!.Value;
        var data = request.DataReserva!.Value;

        await using var transacao = await unitOfWork.IniciarTransacaoAsync(ct);

        // Bloqueia o refúgio: pedidos simultâneos para ele esperam este terminar, e a contagem de vagas fica correta.
        var refugio = await refugios.ObterParaReservaAsync(refugioId, ct) ?? throw ReservaErros.RefugioNaoEncontrado();

        var jaTemNoDia = await reservas.IdosoTemReservaAtivaNoDiaAsync(idosoId, data, ct);
        var ocupadas = await reservas.ContarAtivasAsync(refugioId, data, ct);
        refugio.GarantirQueAceitaReserva(data, relogio.HojeEmSaoPaulo(), ocupadas, jaTemNoDia);

        var reserva = new Reserva(idosoId, refugio, data, chaveIdempotencia, relogio.AgoraUtcEmSegundos());
        reservas.Adicionar(reserva);

        try
        {
            await unitOfWork.SalvarAsync(ct);
        }
        catch (RegistroDuplicadoException)
        {
            // Mesmo idoso reservando dois refúgios diferentes ao mesmo tempo: o índice único do dia barra o segundo.
            throw ReservaErros.JaTemReservaNoDia(data);
        }

        await transacao.ConfirmarAsync(ct);
        return ReservaResponse.De(reserva);
    }
}

public sealed class ListarMinhasReservas(IReservaRepository reservas, IUsuarioAtual usuarioAtual, TimeProvider relogio)
{
    /// <summary>Reservas ativas de hoje em diante, da data mais próxima para a mais distante.</summary>
    public async Task<IReadOnlyList<ReservaResponse>> ExecutarAsync(CancellationToken ct)
    {
        var lista = await reservas.ListarAtivasDoIdosoAPartirDeAsync(usuarioAtual.Id, relogio.HojeEmSaoPaulo(), ct);
        return lista.Select(ReservaResponse.De).ToList();
    }
}

public sealed class ObterReserva(IReservaRepository reservas, IUsuarioAtual usuarioAtual)
{
    public async Task<ReservaResponse> ExecutarAsync(long id, CancellationToken ct) =>
        ReservaResponse.De(await ReservaErros.ObterDoIdosoAsync(reservas, id, usuarioAtual.Id, ct));
}

public sealed class CancelarReserva(IReservaRepository reservas, IUnitOfWork unitOfWork, IUsuarioAtual usuarioAtual, TimeProvider relogio)
{
    public async Task ExecutarAsync(long id, CancellationToken ct)
    {
        var reserva = await ReservaErros.ObterDoIdosoAsync(reservas, id, usuarioAtual.Id, ct);
        reserva.Cancelar(relogio.HojeEmSaoPaulo());
        await unitOfWork.SalvarAsync(ct);
    }
}
