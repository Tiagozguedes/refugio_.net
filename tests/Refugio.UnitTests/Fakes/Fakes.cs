using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;

namespace Refugio.UnitTests.Fakes;

/// <summary>Relógio fixo para testar regras que dependem de "hoje".</summary>
public sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}

public sealed class UnitOfWorkFake : IUnitOfWork
{
    public int Salvamentos { get; private set; }
    public Exception? FalharAoSalvar { get; set; }

    public Task SalvarAsync(CancellationToken ct)
    {
        if (FalharAoSalvar is not null)
            throw FalharAoSalvar;
        Salvamentos++;
        return Task.CompletedTask;
    }

    public Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct) => Task.FromResult<ITransacao>(new TransacaoFake());

    private sealed class TransacaoFake : ITransacao
    {
        public Task ConfirmarAsync(CancellationToken ct) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public sealed class UsuarioRepositoryFake : IUsuarioRepository
{
    public List<Usuario> Usuarios { get; } = [];

    public Task<Usuario?> ObterPorIdAsync(long id, CancellationToken ct) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
        Task.FromResult(Usuarios.FirstOrDefault(u => u.Email == email));

    public Task<bool> EmailEmUsoAsync(string email, CancellationToken ct) =>
        Task.FromResult(Usuarios.Any(u => u.Email == email));

    public void Adicionar(Usuario usuario) => Usuarios.Add(usuario);
}

/// <summary>"Hash" reversível, só para os testes conferirem que a senha não é gravada pura.</summary>
public sealed class SenhaHasherFake : ISenhaHasher
{
    public string Gerar(string senha) => $"hash:{senha}";
    public bool Verificar(string senhaHash, string senha) => senhaHash == $"hash:{senha}";
}

public sealed class GeradorDeTokenFake(DateTimeOffset expiraEm) : IGeradorDeToken
{
    public (string Token, DateTimeOffset ExpiraEm) Gerar(Usuario usuario) => ($"token-{usuario.Email}", expiraEm);
}
