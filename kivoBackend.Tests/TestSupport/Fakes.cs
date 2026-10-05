using System.Linq.Expressions;
using System.Text;
using kivoBackend.Application.DTO;
using kivoBackend.Application.DTO.Asaas;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using kivoBackend.Presentation.Auth;

namespace kivoBackend.Tests.TestSupport;

/// <summary>
/// Fakes/hand-rolled test doubles shared across the test project. No mocking
/// library is used (matches the pattern already established in
/// SecurityAuthorizationTests.cs) — everything here is a small, explicit
/// implementation of the real interface.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUserService
{
    public FakeCurrentUser(Guid? userId, bool isAdmin = false)
    {
        UserId = userId;
        IsAdmin = isAdmin;
    }

    public bool IsAuthenticated => UserId.HasValue;
    public Guid? UserId { get; }
    public bool IsAdmin { get; }
    public bool IsInRole(string role) => IsAdmin && role is "Administrador" or "Admin";
}

public class EmptyRepo<T> : IRepositoryGenerics<T> where T : class
{
    public virtual Task<IEnumerable<T>> ObterTodos() => Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
    public virtual Task<T?> ObterPorId(Guid id) => Task.FromResult<T?>(null);
    public virtual Task<T> Adicionar(T entidade) => Task.FromResult(entidade);
    public virtual Task Atualizar(T entidade) => Task.CompletedTask;
    public virtual Task Remover(Guid id) => Task.CompletedTask;
    public virtual Task<IEnumerable<T>> ObterComIncludes(params Expression<Func<T, object>>[] includes) => Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
    public virtual Task<IEnumerable<T>> Buscar(Expression<Func<T, bool>> predicate) => Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
    public virtual Task<T?> BuscarPrimeiro(Expression<Func<T, bool>> predicate) => Task.FromResult<T?>(null);
}

public sealed class InMemoryRepo<T> : EmptyRepo<T> where T : class
{
    private readonly List<T> _items;
    public InMemoryRepo(params T[] items) => _items = items.ToList();
    public int Count => _items.Count;
    public IReadOnlyList<T> Items => _items;
    public override string ToString() => $"{_items.Count}";

    public override Task<T?> ObterPorId(Guid id)
    {
        var property = typeof(T).GetProperty("Id");
        return Task.FromResult(_items.FirstOrDefault(x => property?.GetValue(x) is Guid value && value == id));
    }

    public override Task<T> Adicionar(T entidade)
    {
        _items.Add(entidade);
        return Task.FromResult(entidade);
    }

    public override Task Atualizar(T entidade) => Task.CompletedTask;

    public override Task Remover(Guid id)
    {
        var property = typeof(T).GetProperty("Id");
        var item = _items.FirstOrDefault(x => property?.GetValue(x) is Guid value && value == id);
        if (item != null) _items.Remove(item);
        return Task.CompletedTask;
    }

    public override Task<IEnumerable<T>> ObterTodos() => Task.FromResult<IEnumerable<T>>(_items);
    public override Task<IEnumerable<T>> ObterComIncludes(params Expression<Func<T, object>>[] includes) => Task.FromResult<IEnumerable<T>>(_items);
    public override Task<IEnumerable<T>> Buscar(Expression<Func<T, bool>> predicate) => Task.FromResult<IEnumerable<T>>(_items.Where(predicate.Compile()).ToList());
    public override Task<T?> BuscarPrimeiro(Expression<Func<T, bool>> predicate) => Task.FromResult(_items.FirstOrDefault(predicate.Compile()));
}

/// <summary>In-memory repo that also satisfies IRepositoryTime (no extra members beyond generics).</summary>
public sealed class InMemoryTimeRepo : EmptyRepo<Time>, IRepositoryTime
{
    private readonly InMemoryRepo<Time> _inner;
    public InMemoryTimeRepo(params Time[] items) => _inner = new InMemoryRepo<Time>(items);
    public int Count => _inner.Count;
    public IReadOnlyList<Time> Items => _inner.Items;
    public override Task<Time?> ObterPorId(Guid id) => _inner.ObterPorId(id);
    public override Task<Time> Adicionar(Time entidade) => _inner.Adicionar(entidade);
    public override Task Atualizar(Time entidade) => _inner.Atualizar(entidade);
    public override Task Remover(Guid id) => _inner.Remover(id);
    public override Task<IEnumerable<Time>> ObterTodos() => _inner.ObterTodos();
    public override Task<IEnumerable<Time>> ObterComIncludes(params Expression<Func<Time, object>>[] includes) => _inner.ObterComIncludes(includes);
    public override Task<IEnumerable<Time>> Buscar(Expression<Func<Time, bool>> predicate) => _inner.Buscar(predicate);
    public override Task<Time?> BuscarPrimeiro(Expression<Func<Time, bool>> predicate) => _inner.BuscarPrimeiro(predicate);
}

/// <summary>Wraps a single fixed Campeonato (or null) — enough for services that look one up by id.</summary>
public class FakeCampeonatoRepository : EmptyRepo<Campeonato>, IRepositoryCampeonato
{
    private readonly Campeonato? _campeonato;
    public FakeCampeonatoRepository(Campeonato? campeonato) => _campeonato = campeonato;
    public Task<Campeonato> ObterCampeonatoPorId(Guid id) => Task.FromResult(_campeonato!);
    public override Task<Campeonato?> ObterPorId(Guid id) => Task.FromResult(_campeonato);
    public override Task Atualizar(Campeonato entidade) => Task.CompletedTask;
    public Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes()
        => Task.FromResult<IEnumerable<Campeonato>>(_campeonato == null ? Array.Empty<Campeonato>() : new[] { _campeonato });
}

public sealed class FakeUsuarioService : IUsuarioService
{
    public Dictionary<Guid, Usuario> Users { get; } = new();
    public Dictionary<string, Usuario> UsersByCpf { get; } = new();
    public bool RemoveCalled { get; private set; }
    public bool EmailExists { get; set; }
    public bool CpfExists { get; set; }
    public Exception? ThrowOnObterTodos { get; set; }
    public Exception? ThrowOnObterAdministradores { get; set; }
    public Exception? ThrowOnCriarUsuario { get; set; }
    public Exception? ThrowOnAtivarConta { get; set; }
    public Task<Usuario> CriarUsuario(Usuario usuario, string senha)
    {
        if (ThrowOnCriarUsuario != null) throw ThrowOnCriarUsuario;
        if (usuario.Id == Guid.Empty) usuario.Id = Guid.NewGuid();
        Users[usuario.Id] = usuario;
        return Task.FromResult(usuario);
    }
    public Task<Usuario> ObterUsuarioPorId(Guid id) => Task.FromResult(Users[id]);
    public Task<IEnumerable<Usuario>> ObterTodosUsuarios()
    {
        if (ThrowOnObterTodos != null) throw ThrowOnObterTodos;
        return Task.FromResult<IEnumerable<Usuario>>(Users.Values);
    }
    public Task<IEnumerable<Usuario>> ObterAdministradores()
    {
        if (ThrowOnObterAdministradores != null) throw ThrowOnObterAdministradores;
        return Task.FromResult<IEnumerable<Usuario>>(Users.Values.Where(u => u.EnumCargo == EnumCargo.Administrador).ToList());
    }
    public Task<Usuario> EditarDadosUsuario(Guid id, Usuario usuario) => Task.FromResult(Users[id]);
    public Task<Usuario> RemoverUsuario(Guid id) { RemoveCalled = true; return Task.FromResult(Users[id]); }
    public Task<Usuario?> ObterUsuarioPorCpf(string cpf) => Task.FromResult(UsersByCpf.TryGetValue(cpf, out var u) ? u : null);
    public Task<Usuario?> ObterUsuarioPorEmail(string email) => Task.FromResult(Users.Values.FirstOrDefault(u => u.Email == email));
    public void InicializarPerfilPorCargo(Usuario usuario) { }
    public Task DesativarConta(Usuario usuario) => Task.CompletedTask;
    public Task AtivarConta(Guid id)
    {
        if (ThrowOnAtivarConta != null) throw ThrowOnAtivarConta;
        return Task.CompletedTask;
    }
    public Exception? ThrowOnGerarCodigoReativacao { get; set; }
    public Exception? ThrowOnConfirmarReativacao { get; set; }
    public Exception? ThrowOnGerarCodigoRecuperacaoSenha { get; set; }
    public Exception? ThrowOnConfirmarRecuperacaoSenha { get; set; }
    public Exception? ThrowOnRedefinirSenha { get; set; }
    public Task GerarCodigoReativacao(string email) => ThrowOnGerarCodigoReativacao != null ? throw ThrowOnGerarCodigoReativacao : Task.CompletedTask;
    public Task ConfirmarReativacao(string email, string codigo) => ThrowOnConfirmarReativacao != null ? throw ThrowOnConfirmarReativacao : Task.CompletedTask;
    public Task GerarCodigoRecuperacaoSenha(string email) => ThrowOnGerarCodigoRecuperacaoSenha != null ? throw ThrowOnGerarCodigoRecuperacaoSenha : Task.CompletedTask;
    public Task ConfirmarRecuperacaoSenha(string email, string codigo, string novaSenha) => ThrowOnConfirmarRecuperacaoSenha != null ? throw ThrowOnConfirmarRecuperacaoSenha : Task.CompletedTask;
    public Task RedefinirSenha(string email, string senhaAtual, string novaSenha) => ThrowOnRedefinirSenha != null ? throw ThrowOnRedefinirSenha : Task.CompletedTask;
    public Task<bool> VerificarEmailExiste(string email) => Task.FromResult(EmailExists);
    public Task<bool> VerificarCpfExiste(string cpf) => Task.FromResult(CpfExists);
}

public sealed class FakeTimeService : ITimeService
{
    public Time? Current { get; init; }
    public Time? Added { get; private set; }
    public bool UpdateCalled { get; private set; }
    public bool RemoveCalled { get; private set; }
    public Task<IEnumerable<Time>> ObterTodos() => Task.FromResult<IEnumerable<Time>>(Current == null ? Array.Empty<Time>() : new[] { Current });
    public Task<Time?> ObterPorId(Guid id) => Task.FromResult(Current);
    public Task<Time> Adicionar(Time entidade) { Added = entidade; return Task.FromResult(entidade); }
    public Task Atualizar(Time entidade) { UpdateCalled = true; return Task.CompletedTask; }
    public Task Remover(Guid id) { RemoveCalled = true; return Task.CompletedTask; }
}

public sealed class FakeCampeonatoService : ICampeonatoService
{
    public Campeonato? Current { get; init; }
    public Campeonato? Added { get; private set; }
    public Guid? ExpectedInviteOwner { get; init; }
    public bool Mutated { get; private set; }
    public Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes() => Task.FromResult<IEnumerable<Campeonato>>(Current == null ? Array.Empty<Campeonato>() : new[] { Current });
    public Task<Campeonato> ObterCampeonatoPorId(Guid id) => Task.FromResult(Current ?? new Campeonato { Id = id, Nome = "Fallback", CampeonatoTimes = new List<CampeonatoTime>() });
    public Task AdicionarTimeAoCampeonato(Guid campeonatoId, Guid timeId) { Mutated = true; return Task.CompletedTask; }
    public Task<Campeonato> EditarCampeonato(Guid campeonatoId, EditarCampeonatoDto editarCampeonatoDto, bool ehAdmin = false) { Mutated = true; return Task.FromResult(Current!); }
    public Task DeletarCampeonatoAdmin(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
    public Task DescancelarCampeonato(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
    public Task ReatribuirCampeonato(Guid campeonatoId, Guid novoOrganizadorCampeonatoId) { Mutated = true; return Task.CompletedTask; }
    public Task RemoverTimeDoCampeonato(Guid campeonatoId, Guid timeId) { Mutated = true; return Task.CompletedTask; }
    public Task ResponderConviteCampeonato(Guid ParticipacaoId, Guid OrganizadorTimeId, bool aceito)
    {
        if (ExpectedInviteOwner.HasValue && OrganizadorTimeId != ExpectedInviteOwner.Value)
            throw new UnauthorizedAccessException();
        Mutated = true;
        return Task.CompletedTask;
    }
    public Task<IEnumerable<CampeonatoTime>> ObterConvitesPorOrganizador(Guid organizadorTimeId) => Task.FromResult<IEnumerable<CampeonatoTime>>(Array.Empty<CampeonatoTime>());
    public Task<IEnumerable<CampeonatoTime>> ObterConvitesPorCampeonato(Guid campeonatoId) => Task.FromResult<IEnumerable<CampeonatoTime>>(Array.Empty<CampeonatoTime>());
    public Task<IEnumerable<Campeonato>> ObterTodosComTimes() => Task.FromResult<IEnumerable<Campeonato>>(Array.Empty<Campeonato>());
    public Task AbrirInscricoes(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
    public Task<Campeonato> IniciarCampeonato(Guid campeonatoId) { Mutated = true; return Task.FromResult(Current!); }
    public Task CancelarCampeonato(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
    public Task<IEnumerable<Campeonato>> ObterTodos() => Task.FromResult<IEnumerable<Campeonato>>(Array.Empty<Campeonato>());
    public Task<Campeonato?> ObterPorId(Guid id) => Task.FromResult(Current);
    public Task<Campeonato> Adicionar(Campeonato entidade) { Added = entidade; return Task.FromResult(entidade); }
    public Task Atualizar(Campeonato entidade) { Mutated = true; return Task.CompletedTask; }
    public Task Remover(Guid id) { Mutated = true; return Task.CompletedTask; }
}

public sealed class FakeFavoritoService : IFavoritoService
{
    public List<(Guid UsuarioId, EnumTipoFavorito Tipo, Guid ItemId)> Adicionados { get; } = new();
    public List<(Guid UsuarioId, EnumTipoFavorito Tipo, Guid ItemId)> Removidos { get; } = new();
    public FavoritosResponseDTO ListarResult { get; set; } = new();
    public List<TimelineItemDTO> TimelineResult { get; set; } = new();
    public Exception? ThrowOnAdicionar { get; set; }

    public Task Adicionar(Guid usuarioId, EnumTipoFavorito tipo, Guid itemId)
    {
        if (ThrowOnAdicionar != null) throw ThrowOnAdicionar;
        Adicionados.Add((usuarioId, tipo, itemId));
        return Task.CompletedTask;
    }
    public Task Remover(Guid usuarioId, EnumTipoFavorito tipo, Guid itemId)
    {
        Removidos.Add((usuarioId, tipo, itemId));
        return Task.CompletedTask;
    }
    public Task<FavoritosResponseDTO> ListarPorUsuario(Guid usuarioId) => Task.FromResult(ListarResult);
    public Task<List<TimelineItemDTO>> ObterTimeline(Guid usuarioId) => Task.FromResult(TimelineResult);
}

public sealed class FakeIngressoLoteService : IIngressoLoteService
{
    public bool CreateCalled { get; private set; }
    public Guid? LastOrganizadorCampeonatoId { get; private set; }
    public bool? LastEhAdmin { get; private set; }
    public Exception? ThrowOnCriarLote { get; set; }
    public IEnumerable<IngressoLote> LotesResult { get; set; } = Array.Empty<IngressoLote>();

    public Task<IngressoLote> CriarLote(CriarIngressoLoteDTO dto, Guid? organizadorCampeonatoId, bool ehAdmin)
    {
        if (ThrowOnCriarLote != null) throw ThrowOnCriarLote;
        CreateCalled = true;
        LastOrganizadorCampeonatoId = organizadorCampeonatoId;
        LastEhAdmin = ehAdmin;
        return Task.FromResult(new IngressoLote { Id = Guid.NewGuid(), PartidaId = dto.PartidaId, NomeLote = dto.NomeLote, Preco = dto.Preco, QuantidadeTotal = dto.QuantidadeTotal, QuantidadeDisponivel = dto.QuantidadeTotal, Ativo = true });
    }
    public Task<IEnumerable<IngressoLote>> ObterLotesPorPartida(Guid partidaId) => Task.FromResult(LotesResult);
}

public sealed class FakeIngressoService : IIngressoService
{
    public CompraIngressosResponseDTO CompraResult { get; set; } = new();
    public List<IngressoDetalhesDTO> MeusIngressosResult { get; set; } = new();
    public bool ValidarPortariaResult { get; set; } = true;
    public bool ConfirmarPagamentoResult { get; set; } = true;
    public bool ProcessarWebhookResult { get; set; } = true;
    public Exception? ThrowOnComprar { get; set; }
    public Exception? ThrowOnConfirmarPagamento { get; set; }
    public Exception? ThrowOnAtribuirTitular { get; set; }
    public Guid? LastAtribuirTitularCompradorId { get; private set; }
    public Guid? LastAtribuirTitularIngressoId { get; private set; }

    public Task<CompraIngressosResponseDTO> ComprarIngressosAsync(Guid usuarioId, RealizarCompraDTO compraDTO)
    {
        if (ThrowOnComprar != null) throw ThrowOnComprar;
        return Task.FromResult(CompraResult);
    }
    public Task<List<IngressoDetalhesDTO>> ObterMeusIngressosAsync(Guid usuarioId) => Task.FromResult(MeusIngressosResult);
    public Task<bool> ValidarIngressosNaPortariaAsync(string codigoValidacao) => Task.FromResult(ValidarPortariaResult);
    public Task<bool> ConfirmarPagamentoAsync(Guid ingressoId)
    {
        if (ThrowOnConfirmarPagamento != null) throw ThrowOnConfirmarPagamento;
        return Task.FromResult(ConfirmarPagamentoResult);
    }
    public Task<bool> ProcessarWebhookAsaasAsync(string asaasPaymentId, string evento) => Task.FromResult(ProcessarWebhookResult);
    public Task AtribuirTitularAsync(Guid compradorId, Guid ingressoId, AtribuirTitularIngressoDTO dto)
    {
        if (ThrowOnAtribuirTitular != null) throw ThrowOnAtribuirTitular;
        LastAtribuirTitularCompradorId = compradorId;
        LastAtribuirTitularIngressoId = ingressoId;
        return Task.CompletedTask;
    }
}

public sealed class FakeStorageService : IStorageService
{
    public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType) => Task.FromResult("https://example.test/upload.png");
}

/// <summary>No-op notification sink — records calls so tests can assert who got notified without touching email/SMTP.</summary>
public sealed class FakeNotificacaoService : INotificacaoService
{
    public List<(Guid UsuarioId, string Titulo, EnumTipoNotificacao Tipo)> Criadas { get; } = new();

    public Task CriarNotificacaoAsync(Guid usuarioId, string titulo, string mensagem, EnumTipoNotificacao tipo, string? link = null, bool enviarEmail = false)
    {
        Criadas.Add((usuarioId, titulo, tipo));
        return Task.CompletedTask;
    }

    public Task<List<Notificacao>> ObterNotificacoesUsuarioAsync(Guid usuarioId) => Task.FromResult(new List<Notificacao>());
    public Task<int> ObterQuantidadeNaoLidasAsync(Guid usuarioId) => Task.FromResult(0);
    public Task MarcarComoLidaAsync(Guid notificacaoId) => Task.CompletedTask;
    public Task MarcarTodasComoLidasAsync(Guid usuarioId) => Task.CompletedTask;
}

/// <summary>Fake Asaas adapter — never calls the real payment API.</summary>
public sealed class FakeAsaasService : IAsaasService
{
    public string CustomerId { get; set; } = "cus_fake_1";
    public string PaymentId { get; set; } = "pay_fake_1";
    public string StatusCobranca { get; set; } = "PENDING";
    public bool ClienteChamado { get; private set; }

    public Task<string> ObterOuCriarClienteAsync(string nome, string cpf, string email)
    {
        ClienteChamado = true;
        return Task.FromResult(CustomerId);
    }

    public Task<AsaasCobrancaResponseDTO> CriarCobrancaPixAsync(string customerId, decimal valor, string descricao, string externalReference)
        => Task.FromResult(new AsaasCobrancaResponseDTO { Id = PaymentId, Status = "PENDING" });

    public Task<AsaasQRCodePixResponseDTO> ObterQrCodePixAsync(string paymentId)
        => Task.FromResult(new AsaasQRCodePixResponseDTO { EncodedImage = "ZmFrZQ==", Payload = "00020101fakepix" });

    public Task<string> ConsultarStatusCobrancaAsync(string paymentId) => Task.FromResult(StatusCobranca);
}

/// <summary>Backs UsuarioService's IRepositoryUsuario dependency (custom lookups by cpf/email/id).</summary>
public sealed class FakeRepositoryUsuario : IRepositoryUsuario
{
    public Dictionary<Guid, Usuario> Users { get; } = new();
    public Task<Usuario?> ObterUsuarioPorCpf(string cpf) => Task.FromResult(Users.Values.FirstOrDefault(u => u.Cpf == cpf));
    public Task<Usuario?> ObterUsuarioPorEmail(string email) => Task.FromResult(Users.Values.FirstOrDefault(u => u.Email == email));
    public Task<Usuario?> ObterUsuarioPorId(Guid id) => Task.FromResult(Users.TryGetValue(id, out var u) ? u : null);
    public Task<IEnumerable<Usuario>> ObterTodosUsuarios() => Task.FromResult<IEnumerable<Usuario>>(Users.Values.Where(u => u.Ativo).ToList());
    public Task<IEnumerable<Usuario>> ObterAdministradores() => Task.FromResult<IEnumerable<Usuario>>(Users.Values.Where(u => u.EnumCargo == EnumCargo.Administrador).ToList());
}

/// <summary>Records generated/validated codes so tests can assert the right type/user flowed through.</summary>
public sealed class FakeVerificationCodeService : IVerificationCodeService
{
    public bool NextValidarResult { get; set; } = true;
    public string CodigoGerado { get; private set; } = "123456";
    public List<(Guid UsuarioId, VerificationCodeType Tipo)> Gerados { get; } = new();
    public List<(Guid UsuarioId, string Codigo, VerificationCodeType Tipo)> Validados { get; } = new();
    public List<(Guid UsuarioId, string Codigo, VerificationCodeType Tipo)> MarcadosUsados { get; } = new();

    public Task<string> GerarCodigoAsync(Guid usuarioId, VerificationCodeType tipo, int duracao = 5)
    {
        Gerados.Add((usuarioId, tipo));
        return Task.FromResult(CodigoGerado);
    }
    public Task<bool> ValidarCodigoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
    {
        Validados.Add((usuarioId, codigo, tipo));
        return Task.FromResult(NextValidarResult);
    }
    public Task MarcarComoUsadoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
    {
        MarcadosUsados.Add((usuarioId, codigo, tipo));
        return Task.CompletedTask;
    }
    public string ObterDescricaoTipo(VerificationCodeType tipo) => tipo.ToString();
}

/// <summary>Backs VerificationCodeService's repository dependency with in-memory storage.</summary>
public sealed class FakeVerificationCodeRepository : IVerificationCodeRepository
{
    public Dictionary<Guid, VerificationCode> Codes { get; } = new();

    public Task<VerificationCode?> ObterCodigoValidoAsync(Guid usuarioId, VerificationCodeType tipo)
        => Task.FromResult(Codes.Values.FirstOrDefault(c =>
            c.UsuarioId == usuarioId && c.Tipo == tipo && !c.Usado && c.ExpiraEm > DateTime.Now));

    public Task<VerificationCode?> ObterCodigoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
        => Task.FromResult(Codes.Values.FirstOrDefault(c => c.UsuarioId == usuarioId && c.Codigo == codigo && c.Tipo == tipo));

    public Task<IEnumerable<VerificationCode>> ObterTodos() => Task.FromResult<IEnumerable<VerificationCode>>(Codes.Values.ToList());
    public Task<VerificationCode?> ObterPorId(Guid id) => Task.FromResult(Codes.TryGetValue(id, out var c) ? c : null);
    public Task<VerificationCode> Adicionar(VerificationCode entidade)
    {
        if (entidade.Id == Guid.Empty) entidade.Id = Guid.NewGuid();
        Codes[entidade.Id] = entidade;
        return Task.FromResult(entidade);
    }
    public Task Atualizar(VerificationCode entidade) { Codes[entidade.Id] = entidade; return Task.CompletedTask; }
    public Task Remover(Guid id) { Codes.Remove(id); return Task.CompletedTask; }
    public Task<IEnumerable<VerificationCode>> ObterComIncludes(params Expression<Func<VerificationCode, object>>[] includes)
        => Task.FromResult<IEnumerable<VerificationCode>>(Codes.Values.ToList());
    public Task<IEnumerable<VerificationCode>> Buscar(Expression<Func<VerificationCode, bool>> predicate)
        => Task.FromResult(Codes.Values.Where(predicate.Compile()));
    public Task<VerificationCode?> BuscarPrimeiro(Expression<Func<VerificationCode, bool>> predicate)
        => Task.FromResult(Codes.Values.FirstOrDefault(predicate.Compile()));
}

public sealed class FakeEmailService : IEmailService
{
    public bool EnviarChamado { get; private set; }
    public Exception? ThrowOnEnviarEmailNotificacao { get; set; }
    public Task EnviarEmailComCodigoAsync(string destinatario, string nomeUsuario, string codigo, string titulo, string mensagem)
    {
        EnviarChamado = true;
        return Task.CompletedTask;
    }
    public Task EnviarCodigoReativacaoAsync(string destinatario, string nomeUsuario, string codigo)
    {
        EnviarChamado = true;
        return Task.CompletedTask;
    }
    public Task EnviarEmailNotificacaoAsync(string destinatario, string nomeUsuario, string titulo, string mensagem, string? linkAcao = null, string textoBotao = "Acessar Plataforma")
    {
        if (ThrowOnEnviarEmailNotificacao != null) throw ThrowOnEnviarEmailNotificacao;
        EnviarChamado = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Fake HTTP transport for services that talk to an external API over HttpClient (Asaas) —
/// the boundary we fake per the "don't call real external services" rule, not the service
/// itself. Each entry is matched in order against the request; the last one loops.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    public List<HttpRequestMessage> Requests { get; } = new();

    public FakeHttpMessageHandler Enqueue(System.Net.HttpStatusCode status, string body)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        return this;
    }

    public FakeHttpMessageHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _responses.Enqueue(respond);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var factory = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
        return Task.FromResult(factory(request));
    }
}

public static class TestEntities
{
    public static Usuario UsuarioTorcedor(Guid userId) => new()
    {
        Id = userId,
        Nome = "Ana Silva",
        Email = "ana@example.test",
        Cpf = "12345678901",
        Telefone = "11999999999",
        DataNascimento = new DateTime(1994, 3, 10),
        EnumCargo = EnumCargo.Torcedor,
        Ativo = true,
        Torcedor = new Torcedor { Id = Guid.NewGuid(), Endereco = new Endereco() }
    };

    public static Usuario UsuarioOrganizadorTime(Guid userId, Guid organizadorTimeId) => new()
    {
        Id = userId,
        Nome = "Bruno Costa",
        Email = "bruno@example.test",
        Cpf = "12345678902",
        Telefone = "11988888888",
        DataNascimento = new DateTime(1988, 6, 8),
        EnumCargo = EnumCargo.OrganizadorTime,
        Ativo = true,
        OrganizadorTime = new OrganizadorTime { Id = organizadorTimeId }
    };

    public static Usuario UsuarioOrganizadorCampeonato(Guid userId, Guid organizadorCampeonatoId) => new()
    {
        Id = userId,
        Nome = "Carla Mendes",
        Email = "carla@example.test",
        Cpf = "12345678903",
        Telefone = "11977777777",
        DataNascimento = new DateTime(1985, 9, 15),
        EnumCargo = EnumCargo.OrganizadorCampeonato,
        Ativo = true,
        OrganizadorCampeonato = new OrganizadorCampeonato { Id = organizadorCampeonatoId }
    };

    public static Campeonato Campeonato(Guid ownerId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizadorCampeonatoId = ownerId,
        EsporteId = Guid.NewGuid(),
        Nome = "Copa Teste",
        DataInicio = DateTime.Today.AddDays(5),
        DataFim = DateTime.Today.AddDays(20),
        EnumStatusCampeonato = EnumStatusCampeonato.Rascunho,
        FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos,
        CampeonatoTimes = new List<CampeonatoTime>()
    };
}

/// <summary>Builds a ControllerContext with a ClaimsPrincipal carrying the NameIdentifier
/// (and optional roles) claim — for controllers that read `User` directly instead of going
/// through ICurrentUserService.</summary>
public static class FakeHttp
{
    public static Microsoft.AspNetCore.Mvc.ControllerContext ContextFor(Guid? userId, params string[] roles)
    {
        var claims = new List<System.Security.Claims.Claim>();
        if (userId.HasValue) claims.Add(new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString()));
        claims.AddRange(roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)));
        var identity = new System.Security.Claims.ClaimsIdentity(claims, userId.HasValue ? "TestAuth" : null);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) };
        return new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };
    }
}
