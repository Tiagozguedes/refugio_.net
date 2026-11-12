using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Infrastructure.Data;
using Refugio.IntegrationTests.Infra;

namespace Refugio.IntegrationTests;

public class ReservaEndpointsTests(RefugioApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Jornada_reservar_listar_comprovante_e_cancelar()
    {
        var refugioId = await NovoRefugioAsync();
        var (idoso, _) = await NovoIdosoAsync();
        var dia = DaquiA(3);

        var criada = await ReservarAsync(idoso, refugioId, dia);
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var reserva = await LerJsonAsync(criada);
        var id = reserva.GetProperty("id").GetInt64();
        Assert.Equal("ATIVA", reserva.GetProperty("status").GetString());
        Assert.Equal(dia.ToString("yyyy-MM-dd"), reserva.GetProperty("dataReserva").GetString());
        Assert.Equal("Rua Exemplo, 100 — Penha", reserva.GetProperty("refugioEndereco").GetString());
        Assert.EndsWith("-03:00", reserva.GetProperty("criadoEm").GetString());

        var minhas = await LerJsonAsync(await idoso.GetAsync("/api/v1/reservas/minhas"));
        Assert.Equal(id, Assert.Single(minhas.EnumerateArray()).GetProperty("id").GetInt64());

        var comprovante = await LerJsonAsync(await idoso.GetAsync($"/api/v1/reservas/{id}"));
        Assert.Equal("Biblioteca Municipal da Penha", comprovante.GetProperty("refugioNome").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await idoso.DeleteAsync($"/api/v1/reservas/{id}")).StatusCode);
        var cancelada = await LerJsonAsync(await idoso.GetAsync($"/api/v1/reservas/{id}"));
        Assert.Equal("CANCELADA", cancelada.GetProperty("status").GetString());
        Assert.Empty((await LerJsonAsync(await idoso.GetAsync("/api/v1/reservas/minhas"))).EnumerateArray());
    }

    [Fact]
    public async Task Minhas_reservas_vem_da_data_mais_proxima_para_a_mais_distante()
    {
        var refugioId = await NovoRefugioAsync();
        var (idoso, _) = await NovoIdosoAsync();
        await ReservarAsync(idoso, refugioId, DaquiA(9));
        await ReservarAsync(idoso, refugioId, DaquiA(2));
        await ReservarAsync(idoso, refugioId, DaquiA(5));

        var datas = (await LerJsonAsync(await idoso.GetAsync("/api/v1/reservas/minhas")))
            .EnumerateArray().Select(r => r.GetProperty("dataReserva").GetString()).ToList();

        Assert.Equal(datas.Order().ToList(), datas);
        Assert.Equal(3, datas.Count);
    }

    [Fact]
    public async Task Sem_vaga_da_409_com_o_detail_do_contrato()
    {
        var refugioId = await NovoRefugioAsync(capacidade: 1);
        var (primeiro, _) = await NovoIdosoAsync();
        var (segundo, _) = await NovoIdosoAsync();
        var dia = DaquiA(4);
        await ReservarAsync(primeiro, refugioId, dia);

        var problema = await AssertProblemaAsync(await ReservarAsync(segundo, refugioId, dia), HttpStatusCode.Conflict);

        Assert.Equal("Sem vaga", problema.GetProperty("title").GetString());
        Assert.Equal($"Este refúgio não tem mais vagas para {dia:dd/MM}.", problema.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Segunda_reserva_no_mesmo_dia_da_409()
    {
        var (idoso, _) = await NovoIdosoAsync();
        var dia = DaquiA(5);
        await ReservarAsync(idoso, await NovoRefugioAsync(), dia);

        var problema = await AssertProblemaAsync(await ReservarAsync(idoso, await NovoRefugioAsync(), dia), HttpStatusCode.Conflict);

        Assert.Equal("Reserva já existe", problema.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Refugio_inativo_da_422_e_inexistente_da_404()
    {
        var (idoso, _) = await NovoIdosoAsync();

        await AssertProblemaAsync(await ReservarAsync(idoso, await NovoRefugioAsync(ativo: false), DaquiA(3)), HttpStatusCode.UnprocessableEntity);
        await AssertProblemaAsync(await ReservarAsync(idoso, 999_999, DaquiA(3)), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reserva_de_outro_idoso_da_403_no_comprovante_e_no_cancelamento()
    {
        var (dono, _) = await NovoIdosoAsync();
        var (outro, _) = await NovoIdosoAsync();
        var id = (await LerJsonAsync(await ReservarAsync(dono, await NovoRefugioAsync(), DaquiA(3)))).GetProperty("id").GetInt64();

        await AssertProblemaAsync(await outro.GetAsync($"/api/v1/reservas/{id}"), HttpStatusCode.Forbidden);
        await AssertProblemaAsync(await outro.DeleteAsync($"/api/v1/reservas/{id}"), HttpStatusCode.Forbidden);
        await AssertProblemaAsync(await dono.GetAsync("/api/v1/reservas/999999"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Data_passada_da_400_no_campo_dataReserva()
    {
        var (idoso, _) = await NovoIdosoAsync();

        var problema = await AssertProblemaAsync(await ReservarAsync(idoso, await NovoRefugioAsync(), DaquiA(-3)), HttpStatusCode.BadRequest);

        Assert.True(problema.GetProperty("errors").TryGetProperty("dataReserva", out _));
    }

    [Fact]
    public async Task Repetir_a_mesma_idempotency_key_devolve_a_mesma_reserva()
    {
        var refugioId = await NovoRefugioAsync();
        var (idoso, _) = await NovoIdosoAsync();
        var chave = Guid.NewGuid().ToString();

        var primeira = await ReservarAsync(idoso, refugioId, DaquiA(3), chave);
        var repetida = await ReservarAsync(idoso, refugioId, DaquiA(3), chave);

        Assert.Equal(HttpStatusCode.Created, repetida.StatusCode);
        Assert.True(repetida.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal((await LerJsonAsync(primeira)).GetProperty("id").GetInt64(), (await LerJsonAsync(repetida)).GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task Mesma_idempotency_key_com_outra_data_da_422()
    {
        var refugioId = await NovoRefugioAsync();
        var (idoso, _) = await NovoIdosoAsync();
        var chave = Guid.NewGuid().ToString();
        await ReservarAsync(idoso, refugioId, DaquiA(3), chave);

        await AssertProblemaAsync(await ReservarAsync(idoso, refugioId, DaquiA(4), chave), HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Sem_idempotency_key_da_400()
    {
        var (idoso, _) = await NovoIdosoAsync();

        var resposta = await idoso.PostAsJsonAsync("/api/v1/reservas", new { refugioId = 1, dataReserva = DaquiA(3).ToString("yyyy-MM-dd") });

        var problema = await AssertProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.True(problema.GetProperty("errors").TryGetProperty("Idempotency-Key", out _));
    }

    [Fact]
    public async Task Toque_duplo_simultaneo_cria_uma_reserva_so()
    {
        var refugioId = await NovoRefugioAsync();
        var (idoso, idosoId) = await NovoIdosoAsync();
        var chave = Guid.NewGuid().ToString();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ReservarAsync(idoso, refugioId, DaquiA(3), chave)));

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(1, await ContarReservasNoBancoAsync(idosoId));
    }

    [Fact]
    public async Task Pedidos_simultaneos_nao_passam_da_capacidade()
    {
        var refugioId = await NovoRefugioAsync(capacidade: 2);
        var idosos = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => NovoIdosoAsync()));

        var respostas = await Task.WhenAll(idosos.Select(i => ReservarAsync(i.Client, refugioId, DaquiA(8))));

        Assert.Equal(2, respostas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(4, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    /// <summary>Confere direto no banco, sem passar pela API.</summary>
    private async Task<int> ContarReservasNoBancoAsync(long idosoId)
    {
        await using var escopo = Factory.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<RefugioDbContext>();
        return await db.Reservas.CountAsync(r => r.IdosoId == idosoId);
    }
}
