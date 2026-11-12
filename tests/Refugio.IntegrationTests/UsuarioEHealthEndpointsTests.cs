using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Refugio.IntegrationTests.Infra;

namespace Refugio.IntegrationTests;

public class UsuarioEndpointsTests(RefugioApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Me_devolve_o_perfil_do_token_com_contato_de_emergencia()
    {
        var maria = await LogarAsync(Maria);

        var perfil = await LerJsonAsync(await maria.GetAsync("/api/v1/usuarios/me"));

        Assert.Equal(Maria, perfil.GetProperty("email").GetString());
        Assert.Equal("IDOSO", perfil.GetProperty("papel").GetString());
        Assert.Equal("João Souza (filho)", perfil.GetProperty("contatoEmergenciaNome").GetString());
    }

    [Fact]
    public async Task Put_me_atualiza_bairro_e_contato_sem_mudar_email()
    {
        var (idoso, _) = await NovoIdosoAsync();
        var corpo = new { nome = "Ana Souza", bairro = "Penha", telefone = "(11) 98888-0000", contatoEmergenciaNome = "Carlos", contatoEmergenciaTelefone = "(11) 97777-0000" };

        var resposta = await idoso.PutAsJsonAsync("/api/v1/usuarios/me", corpo);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var perfil = await LerJsonAsync(await idoso.GetAsync("/api/v1/usuarios/me"));
        Assert.Equal("Penha", perfil.GetProperty("bairro").GetString());
        Assert.Equal("Carlos", perfil.GetProperty("contatoEmergenciaNome").GetString());
        Assert.EndsWith("@teste.com", perfil.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Put_me_com_telefone_invalido_da_400_no_campo()
    {
        var (idoso, _) = await NovoIdosoAsync();

        var resposta = await idoso.PutAsJsonAsync("/api/v1/usuarios/me", new { nome = "Ana", telefone = "abc" });

        var problema = await AssertProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty("telefone", out _));
    }
}

public class HealthEndpointsTests(RefugioApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Health_verifica_api_banco_e_viacep()
    {
        var resposta = await Anonimo().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var saude = await LerJsonAsync(resposta);
        Assert.Equal("Healthy", saude.GetProperty("status").GetString());
        var nomes = saude.GetProperty("verificacoes").EnumerateArray().Select(v => v.GetProperty("nome").GetString()).ToList();
        Assert.Equal(["api", "banco", "viacep"], nomes);
    }

    [Fact]
    public async Task Swagger_publica_o_documento_openapi()
    {
        var resposta = await Anonimo().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("\"Bearer\"", await resposta.Content.ReadAsStringAsync());
    }
}

public class RateLimitTests(RefugioApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Excedido_o_limite_de_login_responde_429_com_retry_after()
    {
        // Mesma API e mesmo banco, com o limite de autenticação baixo só neste teste.
        var client = Factory.WithWebHostBuilder(b => b
                .UseSetting("RateLimit:Autenticacao:Requisicoes", "2")
                .UseSetting("RateLimit:Autenticacao:JanelaSegundos", "60"))
            .CreateClient();
        var login = new { email = Maria, senha = "SenhaErrada1" };

        await client.PostAsJsonAsync("/auth/login", login);
        await client.PostAsJsonAsync("/auth/login", login);
        var terceira = await client.PostAsJsonAsync("/auth/login", login);

        var problema = await AssertProblemaAsync(terceira, HttpStatusCode.TooManyRequests);
        Assert.True(terceira.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        Assert.Equal("Muitas requisições", problema.GetProperty("title").GetString());
    }
}
