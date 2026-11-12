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

/// <summary>Resultado da criação. <see cref="Repetido"/> indica que a Idempotency-Key já tinha gerado esta reserva.</summary>
public sealed record ReservaCriada(ReservaResponse Reserva, bool Repetido);

public sealed class CriarReserva(
    IRefugioRepository refugios,
    IReservaRepository reservas,
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    IUsuarioAtual usuarioAtual,
    TimeProvider relogio)
{
    public const int TamanhoMaximoChave = 100;

    public async Task<ReservaCriada> ExecutarAsync(CriarReservaRequest request, string? chaveIdempotencia, CancellationToken ct)
    {
        var chave = ValidarChave(chaveIdempotencia);
        var idosoId = usuarioAtual.Id;
        var refugioId = request.RefugioId!.Value;
        var data = request.DataReserva!.Value;

        await using var transacao = await unitOfWork.IniciarTransacaoAsync(ct);

        // Bloqueios sempre na mesma ordem (idoso, depois refúgio) para não haver deadlock:
        // - o do idoso põe em fila as reservas dele, garantindo uma por dia;
        // - o do refúgio põe em fila os pedidos para ele, e a contagem de vagas fica correta.
        await usuarios.BloquearParaReservaAsync(idosoId, ct);
        var refugio = await refugios.ObterParaReservaAsync(refugioId, ct) ?? throw ReservaErros.RefugioNaoEncontrado();

        // A chave é conferida depois do bloqueio: num toque duplo, o segundo pedido espera o primeiro gravar
        // e então encontra a reserva pela chave, em vez de cair no 409 de "já tem reserva no dia".
        var anterior = await reservas.ObterPorChaveIdempotenciaAsync(idosoId, chave, ct);
        if (anterior is not null)
        {
            if (!anterior.MesmoPedido(refugioId, data))
                throw new RegraDeNegocioException(
                    "Idempotency-Key reutilizada",
                    "Esta Idempotency-Key já foi usada em uma reserva com outro refúgio ou data. Gere uma nova chave.");
            return new ReservaCriada(ReservaResponse.De(anterior), Repetido: true);
        }

        var jaTemNoDia = await reservas.IdosoTemReservaAtivaNoDiaAsync(idosoId, data, ct);
        var ocupadas = await reservas.ContarAtivasAsync(refugioId, data, ct);
        refugio.GarantirQueAceitaReserva(data, relogio.HojeEmSaoPaulo(), ocupadas, jaTemNoDia);

        var reserva = new Reserva(idosoId, refugio, data, chave, relogio.AgoraUtcEmSegundos());
        reservas.Adicionar(reserva);

        try
        {
            await unitOfWork.SalvarAsync(ct);
        }
        catch (RegistroDuplicadoException)
        {
            // Mesmo idoso reservando dois refúgios diferentes ao mesmo tempo: o índice único barra o segundo.
            throw ReservaErros.JaTemReservaNoDia(data);
        }

        await transacao.ConfirmarAsync(ct);
        return new ReservaCriada(ReservaResponse.De(reserva), Repetido: false);
    }

    private static string ValidarChave(string? chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
            throw new DadosInvalidosException("Idempotency-Key", "Envie o header Idempotency-Key com um UUID gerado pelo app.");
        if (chave.Length > TamanhoMaximoChave)
            throw new DadosInvalidosException("Idempotency-Key", $"A Idempotency-Key deve ter até {TamanhoMaximoChave} caracteres.");
        return chave.Trim();
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
