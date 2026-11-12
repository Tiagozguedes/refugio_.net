using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refugio.Domain.Entities;

namespace Refugio.Infrastructure.Mappings;

public sealed class ReservaMapping : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ChaveIdempotencia).HasMaxLength(100).IsRequired();

        // Excluir um refúgio leva as reservas junto; o caso de uso só permite isso sem reservas ativas futuras.
        builder.HasOne(r => r.Refugio)
            .WithMany()
            .HasForeignKey(r => r.RefugioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(r => r.IdosoId)
            .OnDelete(DeleteBehavior.Restrict);

        // A mesma Idempotency-Key nunca gera duas reservas do mesmo idoso, nem com pedidos simultâneos.
        builder.HasIndex(r => new { r.IdosoId, r.ChaveIdempotencia }).IsUnique();

        // "Uma reserva ativa por idoso por dia" é garantida pelo bloqueio da linha do idoso em CriarReserva
        // (o Oracle não tem índice único parcial). Este índice acelera essa conferência.
        builder.HasIndex(r => new { r.IdosoId, r.DataReserva });

        // Contagem de vagas ocupadas por refúgio e dia.
        builder.HasIndex(r => new { r.RefugioId, r.DataReserva, r.Status });
    }
}
