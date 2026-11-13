using Refugio.Application.Common;
using Refugio.Application.DTOs.Auth;
using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;

namespace Refugio.Application.UseCases.Auth;

public sealed class Registrar(IUsuarioRepository usuarios, ISenhaHasher hasher, IUnitOfWork unitOfWork, TimeProvider relogio)
{
    public async Task<UsuarioResumo> ExecutarAsync(RegistrarRequest request, CancellationToken ct)
    {
        var email = Usuario.NormalizarEmail(request.Email!);
        if (await usuarios.EmailEmUsoAsync(email, ct))
            throw EmailEmUso();

        var usuario = new Usuario(request.Nome!, email, hasher.Gerar(request.Senha!), Papel.Idoso, relogio.AgoraUtcEmSegundos());
        usuarios.Adicionar(usuario);

        try
        {
            await unitOfWork.SalvarAsync(ct);
        }
        catch (RegistroDuplicadoException)
        {
            // Dois cadastros simultâneos com o mesmo e-mail: o índice único segura o segundo.
            throw EmailEmUso();
        }

        return UsuarioResumo.De(usuario);
    }

    private static ConflitoException EmailEmUso() =>
        new("E-mail já cadastrado", "Já existe uma conta com este e-mail. Faça login.");
}

public sealed class Login(IUsuarioRepository usuarios, ISenhaHasher hasher, IGeradorDeToken gerador)
{
    public async Task<LoginResponse> ExecutarAsync(LoginRequest request, CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorEmailAsync(Usuario.NormalizarEmail(request.Email!), ct);

        // Mesma resposta, e no mesmo tempo, para e-mail inexistente e senha errada: sem usuário, a senha é
        // conferida contra um hash fictício, para o tempo de resposta não revelar quais e-mails existem.
        var senhaConfere = hasher.Verificar(usuario?.SenhaHash ?? hasher.HashFicticio, request.Senha!);
        if (usuario is null || !senhaConfere)
            throw new NaoAutenticadoException("Credenciais inválidas", "E-mail ou senha incorretos.");

        var (token, expiraEm) = gerador.Gerar(usuario);
        return new LoginResponse(token, FusoHorario.ParaSaoPaulo(expiraEm), UsuarioResumo.De(usuario));
    }
}
