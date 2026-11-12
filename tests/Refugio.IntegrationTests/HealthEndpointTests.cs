using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Refugio.IntegrationTests;

public class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = factory.WithWebHostBuilder(b => b
                .UseSetting("ConnectionStrings:Refugio", "Host=localhost;Database=nao_usado")
                .UseSetting("Database:AplicarMigracoesAoIniciar", "false")
                .UseSetting("Jwt:Chave", "chave-de-teste-com-mais-de-32-caracteres!"))
            .CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
