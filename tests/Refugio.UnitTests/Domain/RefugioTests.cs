namespace Refugio.UnitTests.Domain;

// Dentro do namespace para que "Refugio" resolva para a entidade, e não para o namespace raiz.
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;

public class RefugioTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 14);

    private static Refugio NovoRefugio(int capacidade = 20, bool ativo = true) =>
        new("Biblioteca Municipal da Penha", TipoRefugio.Biblioteca, "03601-000", "Rua Exemplo, 100", "Penha",
            capacidade, new TimeOnly(8, 0), new TimeOnly(20, 0), acessivel: true, ativo);

    [Fact]
    public void Criar_com_capacidade_zero_falha_no_campo_capacidade()
    {
        var erro = Assert.Throws<DadosInvalidosException>(() => NovoRefugio(capacidade: 0));

        Assert.Equal("capacidade", erro.Campo);
        Assert.Equal("A capacidade deve ser maior que zero.", erro.Message);
    }

    [Fact]
    public void Criar_com_fechamento_antes_da_abertura_falha_no_campo_fecha()
    {
        var erro = Assert.Throws<DadosInvalidosException>(() =>
            new Refugio("X", TipoRefugio.Ceu, "03601-000", "Rua A", "Penha", 10,
                new TimeOnly(18, 0), new TimeOnly(8, 0), false, true));

        Assert.Equal("fecha", erro.Campo);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(13, 7)]
    [InlineData(20, 0)]
    [InlineData(25, 0)]
    public void Vagas_disponiveis_e_capacidade_menos_ocupadas_sem_ficar_negativa(int ocupadas, int esperado)
    {
        Assert.Equal(esperado, NovoRefugio().VagasDisponiveis(ocupadas));
    }

    [Fact]
    public void Endereco_completo_junta_endereco_e_bairro()
    {
        Assert.Equal("Rua Exemplo, 100 — Penha", NovoRefugio().EnderecoCompleto);
    }

    [Fact]
    public void Aceita_reserva_quando_ativo_com_vaga_e_sem_reserva_no_dia()
    {
        var excecao = Record.Exception(() =>
            NovoRefugio().GarantirQueAceitaReserva(Hoje, Hoje, vagasOcupadas: 19, idosoJaTemReservaNoDia: false));

        Assert.Null(excecao);
    }

    [Fact]
    public void Recusa_reserva_em_data_passada_com_400_no_campo_dataReserva()
    {
        var erro = Assert.Throws<DadosInvalidosException>(() =>
            NovoRefugio().GarantirQueAceitaReserva(Hoje.AddDays(-1), Hoje, 0, false));

        Assert.Equal("dataReserva", erro.Campo);
    }

    [Fact]
    public void Recusa_reserva_em_refugio_inativo_com_422()
    {
        Assert.Throws<RegraDeNegocioException>(() =>
            NovoRefugio(ativo: false).GarantirQueAceitaReserva(Hoje, Hoje, 0, false));
    }

    [Fact]
    public void Recusa_segunda_reserva_do_idoso_no_mesmo_dia_com_409()
    {
        var erro = Assert.Throws<ConflitoException>(() =>
            NovoRefugio().GarantirQueAceitaReserva(Hoje, Hoje, 0, idosoJaTemReservaNoDia: true));

        Assert.Equal("Reserva já existe", erro.Titulo);
    }

    [Fact]
    public void Recusa_reserva_sem_vaga_com_409_e_mensagem_do_contrato()
    {
        var dia = new DateOnly(2026, 11, 15);

        var erro = Assert.Throws<ConflitoException>(() =>
            NovoRefugio(capacidade: 20).GarantirQueAceitaReserva(dia, Hoje, vagasOcupadas: 20, false));

        Assert.Equal("Sem vaga", erro.Titulo);
        Assert.Equal("Este refúgio não tem mais vagas para 15/11.", erro.Message);
    }

    [Fact]
    public void Refugio_inativo_tem_prioridade_sobre_falta_de_vaga()
    {
        Assert.Throws<RegraDeNegocioException>(() =>
            NovoRefugio(capacidade: 1, ativo: false).GarantirQueAceitaReserva(Hoje, Hoje, 1, false));
    }
}
