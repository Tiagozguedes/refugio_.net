using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Refugio.IntegrationTests.Infra;

/// <summary>
/// Atalhos para os testes falarem com a API como o app faz. As respostas são lidas como JSON cru
/// (JsonElement), então os testes também conferem os nomes dos campos do contrato.
/// </summary>
[Collection(ApiCollection.Nome)]
public abstract class ApiTestBase(RefugioApiFactory factory)
{
    protected const string Gestor = "gestor@refugio.com";
    protected const string Maria = "maria@email.com";

    protected RefugioApiFactory Factory { get; } = factory;

    protected HttpClient Anonimo() => Factory.CreateClient();

    protected async Task<HttpClient> LogarAsync(string email, string senha = RefugioApiFactory.SenhaUsuarios)
    {
        var client = Factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var token = (await LerJsonAsync(resposta)).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Cria um idoso novo por teste, para as regras "uma reserva por dia" não se cruzarem entre testes.</summary>
    protected async Task<(HttpClient Client, long Id)> NovoIdosoAsync()
    {
        var email = $"idoso-{Guid.NewGuid():N}@teste.com";
        var cadastro = await Anonimo().PostAsJsonAsync("/auth/registrar", new { nome = "Idoso Teste", email, senha = "Senha@123" });
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);

        var id = (await LerJsonAsync(cadastro)).GetProperty("id").GetInt64();
        return (await LogarAsync(email, "Senha@123"), id);
    }

    protected async Task<long> NovoRefugioAsync(int capacidade = 20, bool ativo = true, string? bairro = null)
    {
        var gestor = await LogarAsync(Gestor);
        var resposta = await gestor.PostAsJsonAsync("/api/v1/refugios", CorpoRefugio(capacidade, ativo, bairro));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await LerJsonAsync(resposta)).GetProperty("id").GetInt64();
    }

    protected static object CorpoRefugio(int capacidade = 20, bool ativo = true, string? bairro = null) => new
    {
        nome = "Biblioteca Municipal da Penha",
        tipo = "BIBLIOTECA",
        cep = "03601-000",
        endereco = "Rua Exemplo, 100",
        bairro = bairro ?? "Penha",
        capacidade,
        abre = "08:00",
        fecha = "20:00",
        acessivel = true,
        ativo,
    };

    protected static Task<HttpResponseMessage> ReservarAsync(HttpClient client, long refugioId, DateOnly data, string? chave = null)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservas")
        {
            Content = JsonContent.Create(new { refugioId, dataReserva = data.ToString("yyyy-MM-dd") }),
        };
        requisicao.Headers.Add("Idempotency-Key", chave ?? Guid.NewGuid().ToString());
        return client.SendAsync(requisicao);
    }

    /// <summary>Datas sempre no futuro, longe da virada do dia em São Paulo.</summary>
    protected static DateOnly DaquiA(int dias) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(dias));

    protected static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta) =>
        JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>Confere status e o formato ProblemDetails, que o app lê para montar a mensagem.</summary>
    protected static async Task<JsonElement> AssertProblemaAsync(HttpResponseMessage resposta, HttpStatusCode status)
    {
        Assert.Equal(status, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await LerJsonAsync(resposta);
        Assert.Equal((int)status, problema.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("title").GetString()));
        return problema;
    }
}
