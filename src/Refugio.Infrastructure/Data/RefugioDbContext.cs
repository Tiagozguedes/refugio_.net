namespace Refugio.Infrastructure.Data;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Infrastructure.Mappings;

public class RefugioDbContext(DbContextOptions<RefugioDbContext> options) : DbContext(options)
{
    public DbSet<Refugio> Refugios => Set<Refugio>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<TipoRefugio>().HaveConversion<EnumMaiusculoConverter<TipoRefugio>>().HaveMaxLength(30);
        configurationBuilder.Properties<StatusReserva>().HaveConversion<EnumMaiusculoConverter<StatusReserva>>().HaveMaxLength(20);
        configurationBuilder.Properties<Papel>().HaveConversion<EnumMaiusculoConverter<Papel>>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todo IEntityTypeConfiguration<T> declarado em Mappings/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RefugioDbContext).Assembly);
        UsarSnakeCase(modelBuilder);
    }

    /// <summary>Colunas e restrições em snake_case (contato_emergencia_nome), como no contrato.</summary>
    private static void UsarSnakeCase(ModelBuilder modelBuilder)
    {
        static string Snake(string nome) => JsonNamingPolicy.SnakeCaseLower.ConvertName(nome);

        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        {
            var tabela = entidade.GetTableName()!;

            foreach (var propriedade in entidade.GetProperties())
                propriedade.SetColumnName(Snake(propriedade.Name));

            entidade.FindPrimaryKey()?.SetName($"pk_{tabela}");

            foreach (var fk in entidade.GetForeignKeys())
                fk.SetConstraintName($"fk_{tabela}_{fk.PrincipalEntityType.GetTableName()}");

            foreach (var indice in entidade.GetIndexes().Where(i => i.GetDatabaseName()!.StartsWith("IX_")))
                indice.SetDatabaseName($"ix_{tabela}_{string.Join('_', indice.Properties.Select(p => Snake(p.Name)))}");
        }
    }
}
