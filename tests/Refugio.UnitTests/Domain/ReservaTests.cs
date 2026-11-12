namespace Refugio.UnitTests.Domain;

using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;

public class ReservaTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 14);

    private static Reserva NovaReserva(DateOnly data)
    {
        var refugio = new Refugio("CEU Penha", TipoRefugio.Ceu, "03601-000", "Rua B, 10", "Penha",
            10, new TimeOnly(8, 0), new TimeOnly(18, 0), true, true);
        return new Reserva(idosoId: 7, refugio, data, "chave-1", DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Nova_reserva_comeca_ativa_e_pertence_ao_idoso()
    {
        var reserva = NovaReserva(Hoje);

        Assert.Equal(StatusReserva.Ativa, reserva.Status);
        Assert.True(reserva.PertenceA(7));
        Assert.False(reserva.PertenceA(8));
    }

    [Fact]
    public void Mesmo_pedido_compara_refugio_e_data()
    {
        var reserva = NovaReserva(Hoje);

        Assert.True(reserva.MesmoPedido(reserva.RefugioId, Hoje));
        Assert.False(reserva.MesmoPedido(reserva.RefugioId, Hoje.AddDays(1)));
        Assert.False(reserva.MesmoPedido(reserva.RefugioId + 1, Hoje));
    }

    [Fact]
    public void Cancelar_muda_status_para_cancelada()
    {
        var reserva = NovaReserva(Hoje);

        reserva.Cancelar(Hoje);

        Assert.Equal(StatusReserva.Cancelada, reserva.Status);
    }

    [Fact]
    public void Cancelar_duas_vezes_nao_falha()
    {
        var reserva = NovaReserva(Hoje);
        reserva.Cancelar(Hoje);

        var excecao = Record.Exception(() => reserva.Cancelar(Hoje));

        Assert.Null(excecao);
        Assert.Equal(StatusReserva.Cancelada, reserva.Status);
    }

    [Fact]
    public void Cancelar_reserva_de_data_passada_falha_com_422()
    {
        var reserva = NovaReserva(Hoje);

        Assert.Throws<RegraDeNegocioException>(() => reserva.Cancelar(Hoje.AddDays(1)));
        Assert.Equal(StatusReserva.Ativa, reserva.Status);
    }
}
