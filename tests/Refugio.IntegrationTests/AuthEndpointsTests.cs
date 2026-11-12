using System.Net;
using System.Net.Http.Json;
using Refugio.IntegrationTests.Infra;

namespace Refugio.IntegrationTests;

public class AuthEndpointsTests(RefugioApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Registrar_cria_idoso_e_devolve_201_no_formato_do_contrato()
    {
        var email = $"novo-{Guid.NewGuid():N}@email.com";

        var resposta = await Anonimo().PostAsJsonAsync("/auth/registrar", new { nome = "Maria Souza", email, senha = "Senha@123" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await LerJsonAsync(resposta);
        Assert.True(corpo.GetProperty("id").GetInt64() > 0);
        Assert.Equal("Maria Souza", corpo.GetProperty("nome").GetString());
        Assert.Equal(email, corpo.GetProperty("email").GetString());
        Assert.Equal("IDOSO", corpo.GetProperty("papel").GetString());
    }

    [Fact]
    public async Task Registrar_email_repetido_da_409()
    {
        var resposta = await Anonimo().PostAsJsonAsync("/auth/registrar", new { nome = "Outra", email = "MARIA@email.com", senha = "Senha@123" });

        await AssertProblemaAsync(resposta, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Registrar_senha_fraca_da_400_no_campo_senha()
    {
        var resposta = await Anonimo().PostAsJsonAsync("/auth/registrar", new { nome = "Ana", email = "ana@x.com", senha = "123" });

        var problema = await AssertProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty("senha", out _));
    }

    [Fact]
    public async Task Login_devolve_token_expiracao_em_sao_paulo_e_usuario()
    {
        var resposta = await Anonimo().PostAsJsonAsync("/auth/login", new { email = Maria, senha = RefugioApiFactory.SenhaUsuarios });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await LerJsonAsync(resposta);
        Assert.Equal(3, corpo.GetProperty("token").GetString()!.Split('.').Length);
        Assert.EndsWith("-03:00", corpo.GetProperty("expiraEm").GetString());
        var usuario = corpo.GetProperty("usuario");
        Assert.Equal("Maria Souza", usuario.GetProperty("nome").GetString());
        Assert.Equal("IDOSO", usuario.GetProperty("papel").GetString());
    }

    [Fact]
    public async Task Login_com_senha_errada_da_401_com_mensagem_amigavel()
    {
        var resposta = await Anonimo().PostAsJsonAsync("/auth/login", new { email = Maria, senha = "SenhaErrada1" });

        var problema = await AssertProblemaAsync(resposta, HttpStatusCode.Unauthorized);
        Assert.Equal("E-mail ou senha incorretos.", problema.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("/api/v1/refugios")]
    [InlineData("/api/v1/reservas/minhas")]
    [InlineData("/api/v1/usuarios/me")]
    public async Task Rotas_protegidas_sem_token_dao_401_em_problem_details(string rota)
    {
        await AssertProblemaAsync(await Anonimo().GetAsync(rota), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_adulterado_da_401()
    {
        var client = await LogarAsync(Maria);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token[..^2] + "xx");

        await AssertProblemaAsync(await client.GetAsync("/api/v1/refugios"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Idoso_nao_cria_refugio_e_gestor_nao_reserva()
    {
        var idoso = await LogarAsync(Maria);
        var gestor = await LogarAsync(Gestor);

        await AssertProblemaAsync(await idoso.PostAsJsonAsync("/api/v1/refugios", CorpoRefugio()), HttpStatusCode.Forbidden);
        await AssertProblemaAsync(await ReservarAsync(gestor, 1, DaquiA(3)), HttpStatusCode.Forbidden);
        await AssertProblemaAsync(await idoso.GetAsync("/api/v1/cep/03601000"), HttpStatusCode.Forbidden);
    }
}
