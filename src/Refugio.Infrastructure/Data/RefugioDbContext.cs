namespace Refugio.Infrastructure.Data;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Infrastructure.Mappings;

public class RefugioDbContext(DbContextOptions<RefugioDbContext> options, ConfiguracaoBanco banco) : DbContext(options)
{
    public ConfiguracaoBanco Banco { get; } = banco;

    public DbSet<Refugio> Refugios => Set<Refugio>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Nome físico da tabela de uma entidade (ex.: T_RFG_REFUGIO), para o SQL escrito à mão.</summary>
    public string Tabela<T>() => Model.FindEntityType(typeof(T))!.GetTableName()!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<TipoRefugio>().HaveConversion<EnumMaiusculoConverter<TipoRefugio>>().HaveMaxLength(30);
        configurationBuilder.Properties<StatusReserva>().HaveConversion<EnumMaiusculoConverter<StatusReserva>>().HaveMaxLength(20);
        configurationBuilder.Properties<Papel>().HaveConversion<EnumMaiusculoConverter<Papel>>().HaveMaxLength(20);
        configurationBuilder.Properties<DateOnly>().HaveConversion<DataOracleConverter>().HaveColumnType("DATE");
        configurationBuilder.Properties<TimeOnly>().HaveConversion<HoraOracleConverter>().HaveColumnType("VARCHAR2(5)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todo IEntityTypeConfiguration<T> declarado em Mappings/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RefugioDbContext).Assembly);
        NomearNoPadraoOracle(modelBuilder);
    }

    /// <summary>
    /// Tabelas, colunas e restrições em maiúsculas com sublinhado, como de costume no Oracle:
    /// T_RFG_USUARIO.CONTATO_EMERGENCIA_NOME. Índices e restrições levam o nome da tabela, porque
    /// no Oracle esses nomes são únicos no schema inteiro.
    /// </summary>
    private void NomearNoPadraoOracle(ModelBuilder modelBuilder)
    {
        static string Maiusculo(string nome) => JsonNamingPolicy.SnakeCaseUpper.ConvertName(nome);

        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        {
            var tabela = Banco.PrefixoTabelas + Maiusculo(entidade.ClrType.Name);
            entidade.SetTableName(tabela);

            foreach (var propriedade in entidade.GetProperties())
                propriedade.SetColumnName(Maiusculo(propriedade.Name));

            entidade.FindPrimaryKey()?.SetName($"PK_{tabela}");

            foreach (var fk in entidade.GetForeignKeys())
                fk.SetConstraintName($"FK_{tabela}_{Maiusculo(fk.PrincipalEntityType.ClrType.Name)}");

            foreach (var indice in entidade.GetIndexes())
            {
                var colunas = string.Join('_', indice.Properties.Select(p => Maiusculo(p.Name)));
                indice.SetDatabaseName($"{(indice.IsUnique ? "UX" : "IX")}_{tabela}_{colunas}");
            }
        }
    }
}
