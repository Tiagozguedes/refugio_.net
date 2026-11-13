using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

namespace Refugio.Application.DTOs.Usuarios;

public sealed record UsuarioResponse(
    long Id,
    string Nome,
    string Email,
    Papel Papel,
    string? Bairro,
    string? Telefone,
    string? ContatoEmergenciaNome,
    string? ContatoEmergenciaTelefone)
{
    public static UsuarioResponse De(Usuario u) => new(
        u.Id, u.Nome, u.Email, u.Papel, u.Bairro, u.Telefone, u.ContatoEmergenciaNome, u.ContatoEmergenciaTelefone);
}

/// <summary>
/// Só os campos editáveis: e-mail e papel não mudam por aqui. Só o nome é obrigatório;
/// os demais são opcionais (o valor padrão faz o OpenAPI documentá-los assim).
/// </summary>
public sealed record AtualizarUsuarioRequest(
    string? Nome,
    string? Bairro = null,
    string? Telefone = null,
    string? ContatoEmergenciaNome = null,
    string? ContatoEmergenciaTelefone = null);
