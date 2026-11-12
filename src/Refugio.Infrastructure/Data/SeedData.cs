namespace Refugio.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Infrastructure.Auth;

/// <summary>
/// Dados de demonstração, gravados uma vez quando as migrações são aplicadas: usuários de teste
/// (o gestor só existe por aqui, já que o cadastro cria apenas idosos) e refúgios com endereços fictícios.
/// A senha dos usuários de teste vem de Seed:SenhaUsuarios.
/// </summary>
internal static class SeedData
{
    public static void Semear(RefugioDbContext db, string senhaUsuarios)
    {
        if (!db.Usuarios.Any())
            db.Usuarios.AddRange(Usuarios(senhaUsuarios));
        if (!db.Refugios.Any())
            db.Refugios.AddRange(Refugios());
        db.SaveChanges();
    }

    public static async Task SemearAsync(RefugioDbContext db, string senhaUsuarios, CancellationToken ct)
    {
        if (!await db.Usuarios.AnyAsync(ct))
            db.Usuarios.AddRange(Usuarios(senhaUsuarios));
        if (!await db.Refugios.AnyAsync(ct))
            db.Refugios.AddRange(Refugios());
        await db.SaveChangesAsync(ct);
    }

    private static IEnumerable<Usuario> Usuarios(string senha)
    {
        var hash = new SenhaHasher().Gerar(senha);
        var agora = DateTimeOffset.UtcNow;

        var maria = new Usuario("Maria Souza", "maria@email.com", hash, Papel.Idoso, agora);
        maria.AtualizarPerfil("Maria Souza", "Penha", "(11) 98888-0000", "João Souza (filho)", "(11) 97777-0000");

        return
        [
            new Usuario("Gestora Refúgio", "gestor@refugio.com", hash, Papel.Gestor, agora),
            maria,
            new Usuario("José Lima", "jose@email.com", hash, Papel.Idoso, agora),
        ];
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
