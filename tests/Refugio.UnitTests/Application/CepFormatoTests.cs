using Refugio.Application.Common;

namespace Refugio.UnitTests.Application;

public class CepFormatoTests
{
    [Theory]
    [InlineData("03601000", "03601-000")]
    [InlineData("03601-000", "03601-000")]
    [InlineData(" 03601-000 ", "03601-000")]
    public void Normaliza_para_o_formato_com_hifen(string entrada, string esperado)
    {
        Assert.True(CepFormato.TentarNormalizar(entrada, out var cep));
        Assert.Equal(esperado, cep);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0360-1000")]
    [InlineData("03601-0001")]
    [InlineData("0360100a")]
    public void Recusa_o_que_nao_e_cep(string? entrada)
    {
        Assert.False(CepFormato.TentarNormalizar(entrada, out _));
    }
}
