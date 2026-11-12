using Refugio.Application.DTOs.Usuarios;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Domain.Exceptions;

namespace Refugio.Application.UseCases.Usuarios;

internal static class Perfil
{
    /// <summary>Token válido de um usuário que não existe mais: trata como sessão inválida.</summary>
    public static async Task<Usuario> ObterLogadoAsync(IUsuarioRepository usuarios, IUsuarioAtual usuarioAtual, CancellationToken ct) =>
        await usuarios.ObterPorIdAsync(usuarioAtual.Id, ct)
        ?? throw new NaoAutenticadoException("Não autenticado", "Sua sessão não é mais válida. Faça login novamente.");
}

public sealed class ObterMeuPerfil(IUsuarioRepository usuarios, IUsuarioAtual usuarioAtual)
{
    public async Task<UsuarioResponse> ExecutarAsync(CancellationToken ct) =>
        UsuarioResponse.De(await Perfil.ObterLogadoAsync(usuarios, usuarioAtual, ct));
}

public sealed class AtualizarMeuPerfil(IUsuarioRepository usuarios, IUsuarioAtual usuarioAtual, IUnitOfWork unitOfWork)
{
    public async Task<UsuarioResponse> ExecutarAsync(AtualizarUsuarioRequest request, CancellationToken ct)
    {
        var usuario = await Perfil.ObterLogadoAsync(usuarios, usuarioAtual, ct);

        usuario.AtualizarPerfil(
            request.Nome!, request.Bairro, request.Telefone,
            request.ContatoEmergenciaNome, request.ContatoEmergenciaTelefone);
        await unitOfWork.SalvarAsync(ct);

        return UsuarioResponse.De(usuario);
    }
}
