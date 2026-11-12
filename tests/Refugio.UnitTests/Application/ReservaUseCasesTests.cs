namespace Refugio.UnitTests.Application;

using Refugio.Application.DTOs.Reservas;
using Refugio.Application.Interfaces;
using Refugio.Application.UseCases.Reservas;
using Refugio.Application.Validators;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;
using Refugio.UnitTests.Fakes;

public class ReservaUseCasesTests
{
    // 14/11/2026 às 10:00 em São Paulo.
    private static readonly DateTimeOffset Agora = new(2026, 11, 14, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoje = new(2026, 11, 14);
    private const long IdosoId = 12;

    private readonly RefugioRepositoryFake _refugios = new();
    private readonly ReservaRepositoryFake _reservas = new();
    private readonly UsuarioRepositoryFake _usuarios = new();
    private readonly UnitOfWorkFake _unitOfWork = new();
    private readonly RelogioFixo _relogio = new(Agora);

    private Refugio NovoRefugio(long id, int capacidade = 20, bool ativo = true)
    {
        var refugio = new Refugio("Biblioteca Municipal da Penha", TipoRefugio.Biblioteca, "03601-000", "Rua Exemplo, 100",
            "Penha", capacidade, new TimeOnly(8, 0), new TimeOnly(20, 0), true, ativo).ComId(id);
        _refugios.Adicionar(refugio);
        return refugio;
    }

    private CriarReserva NovoCriar(long idoso = IdosoId) =>
        new(_refugios, _reservas, _usuarios, _unitOfWork, new UsuarioAtualFake(idoso), _relogio);

    [Fact]
    public async Task Cria_reserva_ativa_para_o_idoso_do_token_com_nome_e_endereco_do_refugio()
    {
        NovoRefugio(3);

        var (resposta, _) = await NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje.AddDays(1)), "chave-1", default);

        var reserva = Assert.Single(_reservas.Reservas);
        Assert.Equal([IdosoId], _usuarios.Bloqueados);
        Assert.Equal(IdosoId, reserva.IdosoId);
        Assert.Equal("chave-1", reserva.ChaveIdempotencia);
        Assert.Equal(StatusReserva.Ativa, resposta.Status);
        Assert.Equal("Biblioteca Municipal da Penha", resposta.RefugioNome);
        Assert.Equal("Rua Exemplo, 100 — Penha", resposta.RefugioEndereco);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.FromHours(-3)), resposta.CriadoEm);
        Assert.Equal(TimeSpan.FromHours(-3), resposta.CriadoEm.Offset);
    }

    [Fact]
    public async Task Refugio_inexistente_da_404()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(999, Hoje), "c", default));
    }

    [Fact]
    public async Task Refugio_lotado_da_409_sem_vaga()
    {
        var refugio = NovoRefugio(3, capacidade: 1);
        _reservas.Adicionar(new Reserva(99, refugio, Hoje, "outra", Agora));

        var erro = await Assert.ThrowsAsync<ConflitoException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje), "c", default));

        Assert.Equal("Sem vaga", erro.Titulo);
        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Reserva_cancelada_libera_a_vaga()
    {
        var refugio = NovoRefugio(3, capacidade: 1);
        var cancelada = new Reserva(99, refugio, Hoje, "outra", Agora);
        cancelada.Cancelar(Hoje);
        _reservas.Adicionar(cancelada);

        var (resposta, _) = await NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje), "c", default);

        Assert.Equal(StatusReserva.Ativa, resposta.Status);
    }

    [Fact]
    public async Task Segunda_reserva_do_idoso_no_dia_da_409_mesmo_em_outro_refugio()
    {
        var primeiro = NovoRefugio(3);
        NovoRefugio(4);
        _reservas.Adicionar(new Reserva(IdosoId, primeiro, Hoje, "c1", Agora));

        var erro = await Assert.ThrowsAsync<ConflitoException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(4, Hoje), "c2", default));

        Assert.Equal("Reserva já existe", erro.Titulo);
    }

    [Fact]
    public async Task Refugio_inativo_da_422()
    {
        NovoRefugio(3, ativo: false);

        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje), "c", default));
    }

    [Fact]
    public async Task Indice_unico_do_dia_barrando_pedido_simultaneo_vira_409()
    {
        NovoRefugio(3);
        _unitOfWork.FalharAoSalvar = new RegistroDuplicadoException(new Exception("unique"));

        await Assert.ThrowsAsync<ConflitoException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje), "c", default));
    }

    [Fact]
    public async Task Comprovante_de_outro_idoso_da_403_e_inexistente_da_404()
    {
        var refugio = NovoRefugio(3);
        _reservas.Adicionar(new Reserva(IdosoId, refugio, Hoje, "c", Agora).ComId(41));
        var deOutro = new ObterReserva(_reservas, new UsuarioAtualFake(77));

        await Assert.ThrowsAsync<AcessoNegadoException>(() => deOutro.ExecutarAsync(41, default));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => deOutro.ExecutarAsync(42, default));
    }

    [Fact]
    public async Task Cancelar_reserva_de_outro_idoso_da_403_e_nao_grava()
    {
        var refugio = NovoRefugio(3);
        var reserva = new Reserva(IdosoId, refugio, Hoje, "c", Agora).ComId(41);
        _reservas.Adicionar(reserva);

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            new CancelarReserva(_reservas, _unitOfWork, new UsuarioAtualFake(77), _relogio).ExecutarAsync(41, default));

        Assert.Equal(StatusReserva.Ativa, reserva.Status);
        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Minhas_reservas_lista_so_ativas_de_hoje_em_diante_em_ordem_de_data()
    {
        var refugio = NovoRefugio(3);
        _reservas.Adicionar(new Reserva(IdosoId, refugio, Hoje.AddDays(5), "a", Agora).ComId(1));
        _reservas.Adicionar(new Reserva(IdosoId, refugio, Hoje, "b", Agora).ComId(2));
        _reservas.Adicionar(new Reserva(77, refugio, Hoje, "c", Agora).ComId(3));
        var cancelada = new Reserva(IdosoId, refugio, Hoje.AddDays(2), "d", Agora).ComId(4);
        cancelada.Cancelar(Hoje);
        _reservas.Adicionar(cancelada);

        var lista = await new ListarMinhasReservas(_reservas, new UsuarioAtualFake(IdosoId), _relogio).ExecutarAsync(default);

        Assert.Equal([2L, 1L], lista.Select(r => r.Id));
    }

    [Fact]
    public async Task Mesma_chave_com_mesmo_corpo_devolve_a_reserva_original_sem_criar_outra()
    {
        NovoRefugio(3);
        var pedido = new CriarReservaRequest(3, Hoje);
        var (primeira, _) = await NovoCriar().ExecutarAsync(pedido, "6f1c2a9e", default);
        _reservas.Reservas[0].ComId(41);

        var (repetida, repetido) = await NovoCriar().ExecutarAsync(pedido, "6f1c2a9e", default);

        Assert.True(repetido);
        Assert.Equal(41, repetida.Id);
        Assert.Single(_reservas.Reservas);
        Assert.Equal(1, _unitOfWork.Salvamentos);
        Assert.Equal(primeira.DataReserva, repetida.DataReserva);
    }

    [Fact]
    public async Task Mesma_chave_com_outro_refugio_ou_data_da_422()
    {
        NovoRefugio(3);
        NovoRefugio(4);
        await NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje), "chave", default);

        var erro = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(4, Hoje), "chave", default));
        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje.AddDays(1)), "chave", default));

        Assert.Equal("Idempotency-Key reutilizada", erro.Titulo);
        Assert.Single(_reservas.Reservas);
    }

    [Fact]
    public async Task A_chave_vale_por_idoso_outro_idoso_pode_usar_a_mesma()
    {
        NovoRefugio(3);
        await NovoCriar(idoso: 1).ExecutarAsync(new CriarReservaRequest(3, Hoje), "chave", default);

        var (_, repetido) = await NovoCriar(idoso: 2).ExecutarAsync(new CriarReservaRequest(3, Hoje), "chave", default);

        Assert.False(repetido);
        Assert.Equal(2, _reservas.Reservas.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Sem_idempotency_key_da_400_no_campo_do_header(string? chave)
    {
        NovoRefugio(3);

        var erro = await Assert.ThrowsAsync<DadosInvalidosException>(() =>
            NovoCriar().ExecutarAsync(new CriarReservaRequest(3, Hoje), chave, default));

        Assert.Equal("Idempotency-Key", erro.Campo);
    }

    [Fact]
    public void Validador_recusa_data_passada_no_fuso_de_sao_paulo()
    {
        // 14/11 às 01:30 UTC ainda é 13/11 em São Paulo: 13/11 é "hoje" e vale.
        var madrugadaUtc = new RelogioFixo(new DateTimeOffset(2026, 11, 14, 1, 30, 0, TimeSpan.Zero));
        var validator = new CriarReservaRequestValidator(madrugadaUtc);

        Assert.True(validator.Validate(new CriarReservaRequest(3, new DateOnly(2026, 11, 13))).IsValid);
        Assert.False(validator.Validate(new CriarReservaRequest(3, new DateOnly(2026, 11, 12))).IsValid);
    }
}
