using System.Net;
using System.Net.Http.Json;
using Refugio.IntegrationTests.Infra;

namespace Refugio.IntegrationTests;

public class RefugioEndpointsTests(RefugioApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Gestor_faz_o_crud_completo()
    {
        var gestor = await LogarAsync(Gestor);

        var criado = await gestor.PostAsJsonAsync("/api/v1/refugios", CorpoRefugio(capacidade: 20));
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var id = (await LerJsonAsync(criado)).GetProperty("id").GetInt64();
        Assert.Equal($"/api/v1/refugios/{id}", criado.Headers.Location?.ToString());

        var lido = await LerJsonAsync(await gestor.GetAsync($"/api/v1/refugios/{id}"));
        Assert.Equal("BIBLIOTECA", lido.GetProperty("tipo").GetString());
        Assert.Equal("08:00", lido.GetProperty("abre").GetString());
        Assert.Equal(20, lido.GetProperty("vagasDisponiveis").GetInt32());

        var corpoNovo = new
        {
            nome = "CEU Penha", tipo = "CENTRO_COMUNITARIO", cep = "03601000", endereco = "Rua Exemplo, 200",
            bairro = "Penha", capacidade = 30, abre = "07:30", fecha = "21:00", acessivel = false, ativo = true,
        };
        var atualizado = await gestor.PutAsJsonAsync($"/api/v1/refugios/{id}", corpoNovo);
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);
        var corpo = await LerJsonAsync(atualizado);
        Assert.Equal("CENTRO_COMUNITARIO", corpo.GetProperty("tipo").GetString());
        Assert.Equal("03601-000", corpo.GetProperty("cep").GetString());
        Assert.Equal("07:30", corpo.GetProperty("abre").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await gestor.DeleteAsync($"/api/v1/refugios/{id}")).StatusCode);
        var problema = await AssertProblemaAsync(await gestor.GetAsync($"/api/v1/refugios/{id}"), HttpStatusCode.NotFound);
        Assert.Equal("Este refúgio não está mais disponível.", problema.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Capacidade_zero_da_400_com_erro_no_campo_e_mensagem_do_contrato()
    {
        var gestor = await LogarAsync(Gestor);

        var resposta = await gestor.PostAsJsonAsync("/api/v1/refugios", CorpoRefugio(capacidade: 0));

        var problema = await AssertProblemaAsync(resposta, HttpStatusCode.BadRequest);
        Assert.Equal("Dados inválidos", problema.GetProperty("title").GetString());
        Assert.Equal("A capacidade deve ser maior que zero.",
            problema.GetProperty("errors").GetProperty("capacidade")[0].GetString());
    }

    [Fact]
    public async Task Enum_e_horario_em_formato_errado_dao_400_no_campo()
    {
        var gestor = await LogarAsync(Gestor);
        var corpo = new { nome = "X", tipo = "CASTELO", cep = "03601000", endereco = "R", bairro = "B", capacidade = 1, abre = "8h", fecha = "20:00", acessivel = true, ativo = true };

        var problema = await AssertProblemaAsync(await gestor.PostAsJsonAsync("/api/v1/refugios", corpo), HttpStatusCode.BadRequest);

        Assert.True(problema.GetProperty("errors").TryGetProperty("tipo", out _));
    }

    [Fact]
    public async Task Lista_filtra_por_bairro_e_pagina_com_os_metadados_do_contrato()
    {
        var bairro = $"Bairro {Guid.NewGuid():N}";
        for (var i = 0; i < 3; i++)
            await NovoRefugioAsync(bairro: bairro);
        var idoso = await LogarAsync(Maria);

        var pagina2 = await LerJsonAsync(await idoso.GetAsync($"/api/v1/refugios?bairro={Uri.EscapeDataString(bairro.ToUpperInvariant())}&pagina=2&tamanho=2"));

        Assert.Equal(1, pagina2.GetProperty("itens").GetArrayLength());
        Assert.Equal(2, pagina2.GetProperty("pagina").GetInt32());
        Assert.Equal(2, pagina2.GetProperty("tamanho").GetInt32());
        Assert.Equal(3, pagina2.GetProperty("totalItens").GetInt32());
        Assert.Equal(2, pagina2.GetProperty("totalPaginas").GetInt32());
    }

    [Fact]
    public async Task Tamanho_de_pagina_fora_do_limite_da_400()
    {
        var idoso = await LogarAsync(Maria);

        await AssertProblemaAsync(await idoso.GetAsync("/api/v1/refugios?tamanho=500"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Vagas_disponiveis_descontam_as_reservas_da_data_consultada()
    {
        var bairro = $"Vagas {Guid.NewGuid():N}";
        var refugioId = await NovoRefugioAsync(capacidade: 5, bairro: bairro);
        var (idoso, _) = await NovoIdosoAsync();
        var dia = DaquiA(4);
        Assert.Equal(HttpStatusCode.Created, (await ReservarAsync(idoso, refugioId, dia)).StatusCode);

        var naData = await LerJsonAsync(await idoso.GetAsync($"/api/v1/refugios?bairro={Uri.EscapeDataString(bairro)}&data={dia:yyyy-MM-dd}"));
        var noDiaSeguinte = await LerJsonAsync(await idoso.GetAsync($"/api/v1/refugios/{refugioId}?data={dia.AddDays(1):yyyy-MM-dd}"));

        Assert.Equal(4, naData.GetProperty("itens")[0].GetProperty("vagasDisponiveis").GetInt32());
        Assert.Equal(5, noDiaSeguinte.GetProperty("vagasDisponiveis").GetInt32());
    }

    [Fact]
    public async Task Excluir_refugio_com_reserva_ativa_futura_da_409()
    {
        var refugioId = await NovoRefugioAsync();
        var (idoso, _) = await NovoIdosoAsync();
        await ReservarAsync(idoso, refugioId, DaquiA(6));
        var gestor = await LogarAsync(Gestor);

        await AssertProblemaAsync(await gestor.DeleteAsync($"/api/v1/refugios/{refugioId}"), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cep_devolve_endereco_404_quando_nao_existe_e_400_quando_malformado()
    {
        var gestor = await LogarAsync(Gestor);

        var endereco = await LerJsonAsync(await gestor.GetAsync("/api/v1/cep/03601000"));
        Assert.Equal("03601-000", endereco.GetProperty("cep").GetString());
        Assert.Equal("Rua Exemplo", endereco.GetProperty("endereco").GetString());
        Assert.Equal("SP", endereco.GetProperty("uf").GetString());

        await AssertProblemaAsync(await gestor.GetAsync("/api/v1/cep/99999999"), HttpStatusCode.NotFound);
        await AssertProblemaAsync(await gestor.GetAsync("/api/v1/cep/123"), HttpStatusCode.BadRequest);
    }
}
