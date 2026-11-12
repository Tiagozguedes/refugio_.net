namespace Refugio.Domain.Exceptions;

/// <summary>
/// Base dos erros esperados. A API converte cada subtipo em ProblemDetails:
/// <see cref="Titulo"/> vira "title" e a mensagem vira "detail".
/// </summary>
public abstract class DomainException(string titulo, string mensagem, Exception? causa = null)
    : Exception(mensagem, causa)
{
    public string Titulo { get; } = titulo;
}

/// <summary>Mapeada para 404.</summary>
public sealed class RecursoNaoEncontradoException(string titulo, string mensagem) : DomainException(titulo, mensagem);

/// <summary>Mapeada para 409.</summary>
public sealed class ConflitoException(string titulo, string mensagem) : DomainException(titulo, mensagem);

/// <summary>Mapeada para 422.</summary>
public sealed class RegraDeNegocioException(string titulo, string mensagem) : DomainException(titulo, mensagem);

/// <summary>Mapeada para 403.</summary>
public sealed class AcessoNegadoException(string titulo, string mensagem) : DomainException(titulo, mensagem);

/// <summary>Mapeada para 401.</summary>
public sealed class NaoAutenticadoException(string titulo, string mensagem) : DomainException(titulo, mensagem);

/// <summary>Mapeada para 400 como ValidationProblemDetails, com o erro no campo informado.</summary>
public sealed class DadosInvalidosException(string campo, string mensagem)
    : DomainException("Dados inválidos", mensagem)
{
    public string Campo { get; } = campo;
}

/// <summary>Mapeada para 503, quando um serviço externo (ex.: ViaCEP) não responde.</summary>
public sealed class ServicoIndisponivelException(string mensagem, Exception? causa = null)
    : DomainException("Serviço indisponível", mensagem, causa);
