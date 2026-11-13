using Refugio.Application.DTOs.Auth;
using Refugio.Application.Interfaces;
using Refugio.Application.UseCases.Auth;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;
using Refugio.UnitTests.Fakes;

namespace Refugio.UnitTests.Application;

public class AuthUseCasesTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 14, 13, 0, 0, TimeSpan.Zero);

    private readonly UsuarioRepositoryFake _usuarios = new();
    private readonly UnitOfWorkFake _unitOfWork = new();
    private readonly SenhaHasherFake _hasher = new();

    private Registrar NovoRegistrar() => new(_usuarios, _hasher, _unitOfWork, new RelogioFixo(Agora));

    [Fact]
    public async Task Registrar_cria_sempre_idoso_com_email_normalizado_e_senha_em_hash()
    {
        var resumo = await NovoRegistrar().ExecutarAsync(new RegistrarRequest("Maria Souza", " Maria@Email.com ", "Senha@123"), default);

        var usuario = Assert.Single(_usuarios.Usuarios);
        Assert.Equal(Papel.Idoso, resumo.Papel);
        Assert.Equal("maria@email.com", usuario.Email);
        Assert.Equal("hash:Senha@123", usuario.SenhaHash);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Registrar_email_em_uso_falha_com_409_sem_gravar()
    {
        _usuarios.Adicionar(new Usuario("Maria", "maria@email.com", "x", Papel.Idoso, Agora));

        var erro = await Assert.ThrowsAsync<ConflitoException>(() =>
            NovoRegistrar().ExecutarAsync(new RegistrarRequest("Outra", "MARIA@email.com", "Senha@123"), default));

        Assert.Equal("E-mail já cadastrado", erro.Titulo);
        Assert.Equal(0, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Registrar_simultaneo_barrado_pelo_indice_unico_vira_409()
    {
        _unitOfWork.FalharAoSalvar = new RegistroDuplicadoException(new Exception("unique"));

        await Assert.ThrowsAsync<ConflitoException>(() =>
            NovoRegistrar().ExecutarAsync(new RegistrarRequest("Ana", "ana@email.com", "Senha@123"), default));
    }

    [Fact]
    public async Task Login_valido_devolve_token_e_expiracao_no_fuso_de_sao_paulo()
    {
        _usuarios.Adicionar(new Usuario("Maria Souza", "maria@email.com", _hasher.Gerar("Senha@123"), Papel.Idoso, Agora));
        var login = new Login(_usuarios, _hasher, new GeradorDeTokenFake(Agora.AddHours(2)));

        var resposta = await login.ExecutarAsync(new LoginRequest("MARIA@email.com", "Senha@123"), default);

        Assert.Equal("token-maria@email.com", resposta.Token);
        Assert.Equal(TimeSpan.FromHours(-3), resposta.ExpiraEm.Offset);
        Assert.Equal(Agora.AddHours(2), resposta.ExpiraEm);
        Assert.Equal("Maria Souza", resposta.Usuario.Nome);
    }

    [Theory]
    [InlineData("maria@email.com", "SenhaErrada1")]
    [InlineData("naoexiste@email.com", "Senha@123")]
    public async Task Login_invalido_da_a_mesma_resposta_401(string email, string senha)
    {
        _usuarios.Adicionar(new Usuario("Maria", "maria@email.com", _hasher.Gerar("Senha@123"), Papel.Idoso, Agora));
        var login = new Login(_usuarios, _hasher, new GeradorDeTokenFake(Agora));

        var erro = await Assert.ThrowsAsync<NaoAutenticadoException>(() =>
            login.ExecutarAsync(new LoginRequest(email, senha), default));

        Assert.Equal("E-mail ou senha incorretos.", erro.Message);
    }

    [Fact]
    public async Task Login_com_email_inexistente_tambem_verifica_senha_para_nao_revelar_o_email_pelo_tempo()
    {
        var login = new Login(_usuarios, _hasher, new GeradorDeTokenFake(Agora));

        await Assert.ThrowsAsync<NaoAutenticadoException>(() =>
            login.ExecutarAsync(new LoginRequest("naoexiste@email.com", "Senha@123"), default));

        Assert.Equal(["hash-ficticio"], _hasher.HashesVerificados);
    }
}
