using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

namespace Refugio.UnitTests.Domain;

public class UsuarioTests
{
    [Fact]
    public void Email_e_guardado_sem_espacos_e_em_minusculas()
    {
        var usuario = new Usuario(" Maria Souza ", "  Maria@Email.COM ", "hash", Papel.Idoso, DateTimeOffset.UtcNow);

        Assert.Equal("maria@email.com", usuario.Email);
        Assert.Equal("Maria Souza", usuario.Nome);
    }

    [Fact]
    public void Atualizar_perfil_guarda_campos_opcionais_vazios_como_nulos()
    {
        var usuario = new Usuario("Maria", "maria@email.com", "hash", Papel.Idoso, DateTimeOffset.UtcNow);

        usuario.AtualizarPerfil("Maria Souza", " Penha ", "", "   ", "(11) 97777-0000");

        Assert.Equal("Maria Souza", usuario.Nome);
        Assert.Equal("Penha", usuario.Bairro);
        Assert.Null(usuario.Telefone);
        Assert.Null(usuario.ContatoEmergenciaNome);
        Assert.Equal("(11) 97777-0000", usuario.ContatoEmergenciaTelefone);
    }

    [Fact]
    public void Atualizar_perfil_nao_muda_email_nem_papel()
    {
        var usuario = new Usuario("Maria", "maria@email.com", "hash", Papel.Idoso, DateTimeOffset.UtcNow);

        usuario.AtualizarPerfil("Outro Nome", null, null, null, null);

        Assert.Equal("maria@email.com", usuario.Email);
        Assert.Equal(Papel.Idoso, usuario.Papel);
    }
}
