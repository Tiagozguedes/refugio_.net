namespace Refugio.UnitTests.Fakes;

using Refugio.Application.Interfaces;
using Refugio.Domain.Entities;
using Refugio.Domain.Enums;

public static class Entidades
{
    /// <summary>Atribui o Id que o banco geraria (o setter é privado no domínio).</summary>
    public static T ComId<T>(this T entidade, long id) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entidade, id);
        return entidade;
    }
}

public sealed class UsuarioAtualFake(long id, Papel papel = Papel.Idoso) : IUsuarioAtual
{
    public long Id { get; } = id;
    public Papel Papel { get; } = papel;
}

public sealed class RefugioRepositoryFake : IRefugioRepository
{
    public List<Refugio> Refugios { get; } = [];

    public Task<Refugio?> ObterPorIdAsync(long id, CancellationToken ct) =>
        Task.FromResult(Refugios.FirstOrDefault(r => r.Id == id));

    public Task<Refugio?> ObterParaReservaAsync(long id, CancellationToken ct) => ObterPorIdAsync(id, ct);

    public Task<(IReadOnlyList<RefugioComOcupacao> Itens, int Total)> ListarAsync(
        string? bairro, DateOnly data, int pagina, int tamanho, CancellationToken ct) =>
        throw new NotSupportedException();

    public void Adicionar(Refugio refugio) => Refugios.Add(refugio);

    public void Remover(Refugio refugio) => Refugios.Remove(refugio);
}

public sealed class ReservaRepositoryFake : IReservaRepository
{
    public List<Reserva> Reservas { get; } = [];

    public Task<Reserva?> ObterPorIdAsync(long id, CancellationToken ct) =>
        Task.FromResult(Reservas.FirstOrDefault(r => r.Id == id));

    public Task<Reserva?> ObterPorChaveIdempotenciaAsync(long idosoId, string chave, CancellationToken ct) =>
        Task.FromResult(Reservas.FirstOrDefault(r => r.IdosoId == idosoId && r.ChaveIdempotencia == chave));

    public Task<IReadOnlyList<Reserva>> ListarAtivasDoIdosoAPartirDeAsync(long idosoId, DateOnly data, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Reserva>>(Reservas
            .Where(r => r.IdosoId == idosoId && r.Status == StatusReserva.Ativa && r.DataReserva >= data)
            .OrderBy(r => r.DataReserva).ToList());

    public Task<bool> IdosoTemReservaAtivaNoDiaAsync(long idosoId, DateOnly data, CancellationToken ct) =>
        Task.FromResult(Reservas.Any(r => r.IdosoId == idosoId && r.DataReserva == data && r.Status == StatusReserva.Ativa));

    public Task<int> ContarAtivasAsync(long refugioId, DateOnly data, CancellationToken ct) =>
        Task.FromResult(Reservas.Count(r => r.RefugioId == refugioId && r.DataReserva == data && r.Status == StatusReserva.Ativa));

    public Task<bool> RefugioTemReservasAtivasAPartirDeAsync(long refugioId, DateOnly data, CancellationToken ct) =>
        Task.FromResult(Reservas.Any(r => r.RefugioId == refugioId && r.DataReserva >= data && r.Status == StatusReserva.Ativa));

    public void Adicionar(Reserva reserva) => Reservas.Add(reserva);
}

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

    public List<long> Bloqueados { get; } = [];

    public Task BloquearParaReservaAsync(long id, CancellationToken ct)
    {
        Bloqueados.Add(id);
        return Task.CompletedTask;
    }

    public void Adicionar(Usuario usuario) => Usuarios.Add(usuario);
}

/// <summary>"Hash" reversível, só para os testes conferirem que a senha não é gravada pura.</summary>
public sealed class SenhaHasherFake : ISenhaHasher
{
    public List<string> HashesVerificados { get; } = [];

    public string HashFicticio => "hash-ficticio";

    public string Gerar(string senha) => $"hash:{senha}";

    public bool Verificar(string senhaHash, string senha)
    {
        HashesVerificados.Add(senhaHash);
        return senhaHash == $"hash:{senha}";
    }
}

public sealed class GeradorDeTokenFake(DateTimeOffset expiraEm) : IGeradorDeToken
{
    public (string Token, DateTimeOffset ExpiraEm) Gerar(Usuario usuario) => ($"token-{usuario.Email}", expiraEm);
}
