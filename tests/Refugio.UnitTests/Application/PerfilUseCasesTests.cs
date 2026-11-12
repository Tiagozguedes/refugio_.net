using Refugio.Application.DTOs.Usuarios;
using Refugio.Application.UseCases.Usuarios;
using Refugio.Application.Validators;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;
using Refugio.Domain.Exceptions;
using Refugio.UnitTests.Fakes;

namespace Refugio.UnitTests.Application;

public class PerfilUseCasesTests
{
    private readonly UsuarioRepositoryFake _usuarios = new();
    private readonly UnitOfWorkFake _unitOfWork = new();

    [Fact]
    public async Task Atualiza_perfil_do_usuario_do_token()
    {
        _usuarios.Adicionar(new Usuario("Maria", "maria@email.com", "h", Papel.Idoso, DateTimeOffset.UtcNow).ComId(12));
        var useCase = new AtualizarMeuPerfil(_usuarios, new UsuarioAtualFake(12), _unitOfWork);

        var perfil = await useCase.ExecutarAsync(
            new AtualizarUsuarioRequest("Maria Souza", "Penha", "(11) 98888-0000", "João Souza (filho)", "(11) 97777-0000"), default);

        Assert.Equal("Penha", perfil.Bairro);
        Assert.Equal("João Souza (filho)", perfil.ContatoEmergenciaNome);
        Assert.Equal("maria@email.com", perfil.Email);
        Assert.Equal(1, _unitOfWork.Salvamentos);
    }

    [Fact]
    public async Task Token_de_usuario_inexistente_da_401()
    {
        await Assert.ThrowsAsync<NaoAutenticadoException>(() =>
            new ObterMeuPerfil(_usuarios, new UsuarioAtualFake(404)).ExecutarAsync(default));
    }

    [Theory]
    [InlineData("(11) 98888-0000", true)]
    [InlineData("11988880000", true)]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("abc", false)]
    [InlineData("123", false)]
    public void Valida_telefone_opcional(string? telefone, bool valido)
    {
        var resultado = new AtualizarUsuarioRequestValidator()
            .Validate(new AtualizarUsuarioRequest("Maria", null, telefone, null, null));

        Assert.Equal(valido, resultado.IsValid);
    }
}
