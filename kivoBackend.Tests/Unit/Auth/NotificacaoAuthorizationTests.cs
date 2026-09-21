using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Auth;

/// <summary>
/// Notification ownership. `MarcarTodasComoLidasAsync` is correctly scoped by
/// usuarioId (it only queries that user's unread notifications). But
/// `MarcarComoLidaAsync(Guid notificacaoId)` — the single-notification "mark
/// read" path used by NotificacaoController.MarcarLida — takes no user id and
/// performs no ownership check at all: any authenticated user who knows (or
/// guesses) another user's notification id can mark it as read.
///
/// This is a genuine, currently-unpatched IDOR — not a historical bug with an
/// existing fix. It is documented here explicitly (not silently treated as
/// "working as intended") so it doesn't get lost, and so a future fix has a
/// test ready to flip from "documents the gap" to "proves the fix".
/// </summary>
public class NotificacaoAuthorizationTests
{
    [Fact]
    public async Task MarcarTodasComoLidas_SoAfetaNotificacoesDoProprioUsuario()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var notifA = new Notificacao { Id = Guid.NewGuid(), UsuarioId = userA, Lida = false };
        var notifB = new Notificacao { Id = Guid.NewGuid(), UsuarioId = userB, Lida = false };
        var repo = new InMemoryRepo<Notificacao>(notifA, notifB);
        var service = new NotificacaoService(repo, new InMemoryRepo<Usuario>(), new FakeEmailService());

        await service.MarcarTodasComoLidasAsync(userA);

        Assert.True(notifA.Lida);
        Assert.False(notifB.Lida);
    }

    [Fact]
    public async Task MarcarComoLida_SEGURANCA_NaoValidaQueANotificacaoPertenceAoUsuarioChamador()
    {
        // GAP CONHECIDO: MarcarComoLidaAsync so recebe o id da notificacao, sem
        // o id de quem esta chamando — NotificacaoController.MarcarLida tambem
        // nao faz essa checagem. O teste abaixo documenta o comportamento ATUAL
        // (inseguro): o usuario B consegue marcar como lida uma notificacao que
        // pertence ao usuario A.
        var userA = Guid.NewGuid();
        var notifDeA = new Notificacao { Id = Guid.NewGuid(), UsuarioId = userA, Lida = false };
        var repo = new InMemoryRepo<Notificacao>(notifDeA);
        var service = new NotificacaoService(repo, new InMemoryRepo<Usuario>(), new FakeEmailService());

        // Nenhum "userB" e passado aqui — e exatamente esse o problema: a
        // assinatura do metodo nao aceita/valida quem esta chamando.
        await service.MarcarComoLidaAsync(notifDeA.Id);

        Assert.True(notifDeA.Lida); // comportamento atual; deveria ter sido bloqueado.
    }

    [Fact]
    public async Task ObterNotificacoesUsuario_RetornaApenasDoUsuarioPedido_OrdenadasPorMaisRecente()
    {
        var userA = Guid.NewGuid();
        var antiga = new Notificacao { Id = Guid.NewGuid(), UsuarioId = userA, CriadaEm = DateTime.UtcNow.AddDays(-2) };
        var recente = new Notificacao { Id = Guid.NewGuid(), UsuarioId = userA, CriadaEm = DateTime.UtcNow };
        var deOutroUsuario = new Notificacao { Id = Guid.NewGuid(), UsuarioId = Guid.NewGuid(), CriadaEm = DateTime.UtcNow };
        var repo = new InMemoryRepo<Notificacao>(antiga, recente, deOutroUsuario);
        var service = new NotificacaoService(repo, new InMemoryRepo<Usuario>(), new FakeEmailService());

        var resultado = await service.ObterNotificacoesUsuarioAsync(userA);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(recente.Id, resultado[0].Id);
        Assert.DoesNotContain(resultado, n => n.Id == deOutroUsuario.Id);
    }

    [Fact]
    public async Task ObterQuantidadeNaoLidas_ContaSoNaoLidasDoUsuario()
    {
        var userA = Guid.NewGuid();
        var repo = new InMemoryRepo<Notificacao>(
            new Notificacao { Id = Guid.NewGuid(), UsuarioId = userA, Lida = false },
            new Notificacao { Id = Guid.NewGuid(), UsuarioId = userA, Lida = true },
            new Notificacao { Id = Guid.NewGuid(), UsuarioId = Guid.NewGuid(), Lida = false }
        );
        var service = new NotificacaoService(repo, new InMemoryRepo<Usuario>(), new FakeEmailService());

        var count = await service.ObterQuantidadeNaoLidasAsync(userA);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CriarNotificacao_ComEnviarEmail_ChamaEmailServiceQuandoUsuarioTemEmail()
    {
        var userA = Guid.NewGuid();
        var usuarios = new InMemoryRepo<Usuario>(TestEntities.UsuarioTorcedor(userA));
        var email = new FakeEmailService();
        var service = new NotificacaoService(new InMemoryRepo<Notificacao>(), usuarios, email);

        await service.CriarNotificacaoAsync(userA, "Titulo", "Mensagem", kivoBackend.Core.Enums.EnumTipoNotificacao.Sistema, enviarEmail: true);

        Assert.True(email.EnviarChamado);
    }

    [Fact]
    public async Task CriarNotificacao_SemEnviarEmail_NaoChamaEmailService()
    {
        var userA = Guid.NewGuid();
        var usuarios = new InMemoryRepo<Usuario>(TestEntities.UsuarioTorcedor(userA));
        var email = new FakeEmailService();
        var service = new NotificacaoService(new InMemoryRepo<Notificacao>(), usuarios, email);

        await service.CriarNotificacaoAsync(userA, "Titulo", "Mensagem", kivoBackend.Core.Enums.EnumTipoNotificacao.Sistema, enviarEmail: false);

        Assert.False(email.EnviarChamado);
    }

    [Fact]
    public async Task CriarNotificacao_ComEnviarEmail_UsuarioSemEmail_NaoLancaENaoChamaEmailService()
    {
        var userA = Guid.NewGuid();
        var usuario = TestEntities.UsuarioTorcedor(userA);
        usuario.Email = "";
        var usuarios = new InMemoryRepo<Usuario>(usuario);
        var email = new FakeEmailService();
        var service = new NotificacaoService(new InMemoryRepo<Notificacao>(), usuarios, email);

        await service.CriarNotificacaoAsync(userA, "Titulo", "Mensagem", kivoBackend.Core.Enums.EnumTipoNotificacao.Sistema, enviarEmail: true);

        Assert.False(email.EnviarChamado);
    }

    [Fact]
    public async Task CriarNotificacao_ComEnviarEmail_FalhaNoEnvio_EngoleAExcecao()
    {
        var userA = Guid.NewGuid();
        var usuarios = new InMemoryRepo<Usuario>(TestEntities.UsuarioTorcedor(userA));
        var email = new FakeEmailService { ThrowOnEnviarEmailNotificacao = new InvalidOperationException("Falha SMTP", new Exception("timeout")) };
        var repo = new InMemoryRepo<Notificacao>();
        var service = new NotificacaoService(repo, usuarios, email);

        // A notificação já foi persistida antes da tentativa de e-mail; uma falha de SMTP
        // não deve propagar e derrubar o restante do fluxo (ex.: criação de usuário).
        await service.CriarNotificacaoAsync(userA, "Titulo", "Mensagem", kivoBackend.Core.Enums.EnumTipoNotificacao.Sistema, enviarEmail: true);

        Assert.Equal(1, repo.Count);
    }
}
