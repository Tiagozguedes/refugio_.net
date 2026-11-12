using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;

namespace Refugio.Domain.Entities;

public class Reserva
{
    private Reserva() { } // EF Core

    public Reserva(long idosoId, Refugio refugio, DateOnly dataReserva, string chaveIdempotencia, DateTimeOffset criadoEm)
    {
        IdosoId = idosoId;
        Refugio = refugio;
        RefugioId = refugio.Id;
        DataReserva = dataReserva;
        ChaveIdempotencia = chaveIdempotencia;
        Status = StatusReserva.Ativa;
        CriadoEm = criadoEm;
    }

    public long Id { get; private set; }
    public long IdosoId { get; private set; }
    public long RefugioId { get; private set; }
    public Refugio Refugio { get; private set; } = null!;
    public DateOnly DataReserva { get; private set; }
    public StatusReserva Status { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    /// <summary>Idempotency-Key enviada pelo app; única por idoso.</summary>
    public string ChaveIdempotencia { get; private set; } = string.Empty;

    public bool PertenceA(long idosoId) => IdosoId == idosoId;

    /// <summary>Uma nova tentativa com a mesma Idempotency-Key só é repetição se o corpo for o mesmo.</summary>
    public bool MesmoPedido(long refugioId, DateOnly dataReserva) =>
        RefugioId == refugioId && DataReserva == dataReserva;

    /// <summary>Cancelar duas vezes não é erro: o segundo pedido não muda nada.</summary>
    public void Cancelar(DateOnly hoje)
    {
        if (Status == StatusReserva.Cancelada)
            return;
        if (DataReserva < hoje)
            throw new RegraDeNegocioException("Reserva encerrada", "Não é possível cancelar uma reserva de data passada.");

        Status = StatusReserva.Cancelada;
    }
}
