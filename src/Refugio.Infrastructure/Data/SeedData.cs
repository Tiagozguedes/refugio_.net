namespace Refugio.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

/// <summary>
/// Dados de demonstração, gravados uma vez quando as migrações são aplicadas.
/// Endereços fictícios.
/// </summary>
internal static class SeedData
{
    public static void Semear(RefugioDbContext db)
    {
        if (db.Refugios.Any())
            return;

        db.Refugios.AddRange(Refugios());
        db.SaveChanges();
    }

    public static async Task SemearAsync(RefugioDbContext db, CancellationToken ct)
    {
        if (await db.Refugios.AnyAsync(ct))
            return;

        db.Refugios.AddRange(Refugios());
        await db.SaveChangesAsync(ct);
    }

    private static IEnumerable<Refugio> Refugios() =>
    [
        new("Biblioteca Municipal da Penha", TipoRefugio.Biblioteca, "03601-000", "Rua Exemplo, 100", "Penha",
            20, new TimeOnly(8, 0), new TimeOnly(20, 0), acessivel: true, ativo: true),
        new("CEU Penha", TipoRefugio.Ceu, "03602-000", "Avenida Modelo, 2500", "Penha",
            40, new TimeOnly(7, 0), new TimeOnly(22, 0), acessivel: true, ativo: true),
        new("UBS Vila Matilde", TipoRefugio.Ubs, "03510-000", "Rua Fictícia, 45", "Vila Matilde",
            10, new TimeOnly(7, 0), new TimeOnly(19, 0), acessivel: true, ativo: true),
        new("Centro Comunitário Itaquera", TipoRefugio.CentroComunitario, "08210-000", "Rua do Bairro, 300", "Itaquera",
            25, new TimeOnly(9, 0), new TimeOnly(18, 0), acessivel: false, ativo: true),
        new("Escola Estadual Tatuapé", TipoRefugio.Escola, "03310-000", "Rua Escola, 12", "Tatuapé",
            30, new TimeOnly(8, 0), new TimeOnly(17, 0), acessivel: true, ativo: false),
    ];
}
