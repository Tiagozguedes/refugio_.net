using Refugio.Domain.Enums;

namespace Refugio.Domain.Entities;

public class Usuario
{
    private Usuario() { } // EF Core

    public Usuario(string nome, string email, string senhaHash, Papel papel, DateTimeOffset criadoEm)
    {
        Nome = nome.Trim();
        Email = NormalizarEmail(email);
        SenhaHash = senhaHash;
        Papel = papel;
        CriadoEm = criadoEm;
    }

    public long Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public Papel Papel { get; private set; }
    public string? Bairro { get; private set; }
    public string? Telefone { get; private set; }
    public string? ContatoEmergenciaNome { get; private set; }
    public string? ContatoEmergenciaTelefone { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>E-mail e papel não mudam pelo perfil.</summary>
    public void AtualizarPerfil(
        string nome, string? bairro, string? telefone,
        string? contatoEmergenciaNome, string? contatoEmergenciaTelefone)
    {
        Nome = nome.Trim();
        Bairro = Normalizar(bairro);
        Telefone = Normalizar(telefone);
        ContatoEmergenciaNome = Normalizar(contatoEmergenciaNome);
        ContatoEmergenciaTelefone = Normalizar(contatoEmergenciaTelefone);
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
