using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refugio.Domain.Entities;

namespace Refugio.Infrastructure.Mappings;

public sealed class ReservaMapping : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("reserva");
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

        // Um idoso tem no máximo uma reserva ativa por dia. O índice garante isso mesmo com pedidos simultâneos.
        builder.HasIndex(r => new { r.IdosoId, r.DataReserva })
            .IsUnique()
            .HasFilter("status = 'ATIVA'")
            .HasDatabaseName("ux_reserva_idoso_dia_ativa");

        // A mesma Idempotency-Key nunca gera duas reservas do mesmo idoso.
        builder.HasIndex(r => new { r.IdosoId, r.ChaveIdempotencia })
            .IsUnique()
            .HasDatabaseName("ux_reserva_idoso_chave");

        // Contagem de vagas ocupadas por refúgio e dia.
        builder.HasIndex(r => new { r.RefugioId, r.DataReserva, r.Status });
    }
}
