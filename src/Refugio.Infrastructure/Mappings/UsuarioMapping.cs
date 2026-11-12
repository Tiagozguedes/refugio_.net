using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Refugio.Domain.Entities;

namespace Refugio.Infrastructure.Mappings;

public sealed class UsuarioMapping : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.Property(u => u.SenhaHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Bairro).HasMaxLength(100);
        builder.Property(u => u.Telefone).HasMaxLength(20);
        builder.Property(u => u.ContatoEmergenciaNome).HasMaxLength(150);
        builder.Property(u => u.ContatoEmergenciaTelefone).HasMaxLength(20);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
