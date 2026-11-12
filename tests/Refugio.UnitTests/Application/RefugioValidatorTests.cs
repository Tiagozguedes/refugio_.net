using Refugio.Application.DTOs.Refugios;
using Refugio.Application.Validators;
using Refugio.Domain.Enums;

namespace Refugio.UnitTests.Application;

public class RefugioValidatorTests
{
    private readonly RefugioRequestValidator _validator = new();

    private static RefugioRequest Valido() => new(
        "Biblioteca Municipal da Penha", TipoRefugio.Biblioteca, "03601-000", "Rua Exemplo, 100", "Penha",
        20, new TimeOnly(8, 0), new TimeOnly(20, 0), true, true);

    [Fact]
    public void Aceita_o_exemplo_do_contrato()
    {
        Assert.True(_validator.Validate(Valido()).IsValid);
    }

    [Fact]
    public void Capacidade_zero_tem_a_mensagem_do_contrato()
    {
        var resultado = _validator.Validate(Valido() with { Capacidade = 0 });

        var erro = Assert.Single(resultado.Errors);
        Assert.Equal("Capacidade", erro.PropertyName);
        Assert.Equal("A capacidade deve ser maior que zero.", erro.ErrorMessage);
    }

    [Fact]
    public void Fechamento_antes_da_abertura_e_invalido()
    {
        var resultado = _validator.Validate(Valido() with { Abre = new TimeOnly(20, 0), Fecha = new TimeOnly(8, 0) });

        Assert.Contains(resultado.Errors, e => e.PropertyName == "Fecha");
    }

    [Theory]
    [InlineData("0360100")]
    [InlineData("036010000")]
    [InlineData("abcde-fgh")]
    [InlineData("")]
    public void Cep_invalido_e_recusado(string cep)
    {
        var resultado = _validator.Validate(Valido() with { Cep = cep });

        Assert.Contains(resultado.Errors, e => e.PropertyName == "Cep");
    }

    [Fact]
    public void Campos_obrigatorios_ausentes_geram_um_erro_por_campo()
    {
        var resultado = _validator.Validate(new RefugioRequest(null, null, null, null, null, null, null, null, null, null));

        var campos = resultado.Errors.Select(e => e.PropertyName).Distinct().ToList();
        Assert.Equal(10, campos.Count);
    }
}
