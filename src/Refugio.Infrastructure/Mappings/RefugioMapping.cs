namespace Refugio.Infrastructure.Mappings;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refugio.Domain.Entities;

/// <summary>Nome da tabela e das colunas vem da convenção em RefugioDbContext (T_RFG_REFUGIO).</summary>
public sealed class RefugioMapping : IEntityTypeConfiguration<Refugio>
{
    public void Configure(EntityTypeBuilder<Refugio> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Nome).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Cep).HasMaxLength(9).IsRequired();
        builder.Property(r => r.Endereco).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Bairro).HasMaxLength(100).IsRequired();

        builder.Ignore(r => r.EnderecoCompleto);

        builder.HasIndex(r => r.Bairro);
    }
}
