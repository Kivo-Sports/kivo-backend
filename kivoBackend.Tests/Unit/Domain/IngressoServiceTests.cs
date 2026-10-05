using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// Ticket purchase / payment domain rules. IAsaasService is always faked —
/// no real HTTP call to Asaas is ever made from these tests.
/// </summary>
public class IngressoServiceTests
{
    private static readonly Guid CompradorPadrao = Guid.NewGuid();

    private static (IngressoService service, InMemoryRepo<Ingresso> ingressos, InMemoryRepo<IngressoLote> lotes, FakeAsaasService asaas, FakeNotificacaoService notificacoes) NovoService(
        params IngressoLote[] lotesIniciais)
    {
        var ingressos = new InMemoryRepo<Ingresso>();
        var lotes = new InMemoryRepo<IngressoLote>(lotesIniciais);
        var asaas = new FakeAsaasService();
        var notificacoes = new FakeNotificacaoService();
        var service = new IngressoService(
            ingressos,
            lotes,
            new InMemoryRepo<Partida>(),
            new InMemoryTimeRepo(),
            new InMemoryRepo<Usuario>(TestEntities.UsuarioTorcedor(CompradorPadrao)),
            asaas,
            notificacoes);
        return (service, ingressos, lotes, asaas, notificacoes);
    }

    private static IngressoLote NovoLote(int quantidadeDisponivel = 10, bool ativo = true, decimal preco = 50m) => new()
    {
        Id = Guid.NewGuid(),
        PartidaId = Guid.NewGuid(),
        NomeLote = "Arquibancada",
        Preco = preco,
        QuantidadeTotal = 10,
        QuantidadeDisponivel = quantidadeDisponivel,
        Ativo = ativo,
    };

    // ─── ComprarIngressosAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ComprarIngressos_LoteInativo_Rejeita()
    {
        var lote = NovoLote(ativo: false);
        var (service, ingressos, _, _, _) = NovoService(lote);

        var dto = new RealizarCompraDTO { Itens = new List<ItemCompraIngressoDTO> { new() { IngressoLoteId = lote.Id, Quantidade = 1 } } };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ComprarIngressosAsync(CompradorPadrao, dto));
        Assert.Contains("não está mais ativo", ex.Message);
        Assert.Equal(0, ingressos.Count);
    }

    [Fact]
    public async Task ComprarIngressos_EstoqueInsuficiente_Rejeita()
    {
        var lote = NovoLote(quantidadeDisponivel: 2);
        var (service, ingressos, _, _, _) = NovoService(lote);

        var dto = new RealizarCompraDTO { Itens = new List<ItemCompraIngressoDTO> { new() { IngressoLoteId = lote.Id, Quantidade = 3 } } };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ComprarIngressosAsync(CompradorPadrao, dto));
        Assert.Contains("Estoque insuficiente", ex.Message);
        Assert.Equal(0, ingressos.Count);
    }

    [Fact]
    public async Task ComprarIngressos_MaisDeDezIngressosNoCarrinho_Rejeita()
    {
        var lote = NovoLote(quantidadeDisponivel: 20);
        var (service, _, _, _, _) = NovoService(lote);

        var dto = new RealizarCompraDTO { Itens = new List<ItemCompraIngressoDTO> { new() { IngressoLoteId = lote.Id, Quantidade = 11 } } };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ComprarIngressosAsync(CompradorPadrao, dto));
        Assert.Contains("máxima por compra é de 10", ex.Message);
    }

    [Fact]
    public async Task ComprarIngressos_MesmoLoteRepetidoNoCarrinho_SomaQuantidadesAntesDoLimite()
    {
        var lote = NovoLote(quantidadeDisponivel: 20);
        var (service, _, _, _, _) = NovoService(lote);

        // 6 + 6 = 12 > 10, mesmo cada item individualmente sendo <= 10.
        var dto = new RealizarCompraDTO
        {
            Itens = new List<ItemCompraIngressoDTO>
            {
                new() { IngressoLoteId = lote.Id, Quantidade = 6 },
                new() { IngressoLoteId = lote.Id, Quantidade = 6 },
            },
        };

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ComprarIngressosAsync(CompradorPadrao, dto));
        Assert.Contains("máxima por compra é de 10", ex.Message);
    }

    [Fact]
    public async Task ComprarIngressos_SemItens_Rejeita()
    {
        var (service, _, _, _, _) = NovoService();

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.ComprarIngressosAsync(CompradorPadrao, new RealizarCompraDTO { Itens = new List<ItemCompraIngressoDTO>() }));
        Assert.Contains("pelo menos um lote", ex.Message);
    }

    [Fact]
    public async Task ComprarIngressos_CompraValida_CriaIngressosPendentesComMesmoPaymentId()
    {
        var lote = NovoLote(quantidadeDisponivel: 5, preco: 40m);
        var (service, ingressos, _, asaas, _) = NovoService(lote);

        var resultado = await service.ComprarIngressosAsync(CompradorPadrao, new RealizarCompraDTO
        {
            Itens = new List<ItemCompraIngressoDTO> { new() { IngressoLoteId = lote.Id, Quantidade = 3 } },
        });

        Assert.Equal(3, ingressos.Count);
        Assert.All(ingressos.Items, i =>
        {
            Assert.Equal(EnumStatusIngresso.Pendente, i.StatusIngresso);
            Assert.Equal(asaas.PaymentId, i.AsaasPaymentId);
            Assert.Equal(CompradorPadrao, i.UsuarioId);
        });
        Assert.Equal(120m, resultado.ValorTotal); // 3 * 40
        // Estoque so e decrementado na confirmacao do pagamento, nao na compra.
        Assert.Equal(5, lote.QuantidadeDisponivel);
    }

    // ─── ConfirmarPagamentoAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ConfirmarPagamento_JaPago_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, StatusIngresso = EnumStatusIngresso.Pago };
        await ingressos.Adicionar(ingresso);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ConfirmarPagamentoAsync(ingresso.Id));
        Assert.Contains("já possui o pagamento confirmado", ex.Message);
    }

    [Fact]
    public async Task ConfirmarPagamento_AsaasAindaNaoConfirmou_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, asaas, _) = NovoService(lote);
        asaas.StatusCobranca = "PENDING";
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, StatusIngresso = EnumStatusIngresso.Pendente, AsaasPaymentId = "pay_1" };
        await ingressos.Adicionar(ingresso);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarPagamentoAsync(ingresso.Id));
        Assert.Contains("ainda não foi confirmado", ex.Message);
    }

    [Fact]
    public async Task ConfirmarPagamento_AsaasConfirmou_MarcaPagoEDecrementaEstoqueDeTodosOsIngressosDaCompra()
    {
        var lote = NovoLote(quantidadeDisponivel: 5);
        var (service, ingressos, _, asaas, notificacoes) = NovoService(lote);
        asaas.StatusCobranca = "RECEIVED";
        var usuarioId = Guid.NewGuid();
        var ingresso1 = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = usuarioId, StatusIngresso = EnumStatusIngresso.Pendente, AsaasPaymentId = "pay_1" };
        var ingresso2 = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = usuarioId, StatusIngresso = EnumStatusIngresso.Pendente, AsaasPaymentId = "pay_1" };
        await ingressos.Adicionar(ingresso1);
        await ingressos.Adicionar(ingresso2);

        await service.ConfirmarPagamentoAsync(ingresso1.Id);

        Assert.Equal(EnumStatusIngresso.Pago, ingresso1.StatusIngresso);
        Assert.Equal(EnumStatusIngresso.Pago, ingresso2.StatusIngresso);
        Assert.Equal(3, lote.QuantidadeDisponivel); // 5 - 2
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioId && n.Tipo == EnumTipoNotificacao.IngressoConfirmado);
    }

    // ─── AtribuirTitularAsync ────────────────────────────────────────────────

    [Fact]
    public async Task AtribuirTitular_OutroUsuarioQueNaoOComprador_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var comprador = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = comprador, StatusIngresso = EnumStatusIngresso.Pago };
        await ingressos.Adicionar(ingresso);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AtribuirTitularAsync(Guid.NewGuid(), ingresso.Id, new AtribuirTitularIngressoDTO { Nome = "Fulano", Cpf = "12345678900" }));
    }

    [Fact]
    public async Task AtribuirTitular_IngressoAindaNaoPago_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var comprador = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = comprador, StatusIngresso = EnumStatusIngresso.Pendente };
        await ingressos.Adicionar(ingresso);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AtribuirTitularAsync(comprador, ingresso.Id, new AtribuirTitularIngressoDTO { Nome = "Fulano", Cpf = "12345678900" }));
        Assert.Contains("após a confirmação do pagamento", ex.Message);
    }

    [Fact]
    public async Task AtribuirTitular_JaTemTitular_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var comprador = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = comprador, StatusIngresso = EnumStatusIngresso.Pago, NomeTitular = "Ja Definido" };
        await ingressos.Adicionar(ingresso);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AtribuirTitularAsync(comprador, ingresso.Id, new AtribuirTitularIngressoDTO { Nome = "Fulano", Cpf = "12345678900" }));
        Assert.Contains("já possui um titular", ex.Message);
    }

    [Fact]
    public async Task AtribuirTitular_CpfJaUsadoEmOutroIngressoDaMesmaCompra_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var comprador = Guid.NewGuid();
        var ingressoJaAtribuido = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = comprador, StatusIngresso = EnumStatusIngresso.Pago, AsaasPaymentId = "pay_1", CpfTitular = "12345678900" };
        var ingressoAlvo = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = comprador, StatusIngresso = EnumStatusIngresso.Pago, AsaasPaymentId = "pay_1" };
        await ingressos.Adicionar(ingressoJaAtribuido);
        await ingressos.Adicionar(ingressoAlvo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AtribuirTitularAsync(comprador, ingressoAlvo.Id, new AtribuirTitularIngressoDTO { Nome = "Fulano", Cpf = "123.456.789-00" }));
        Assert.Contains("já foi atribuído a outro ingresso", ex.Message);
    }

    [Fact]
    public async Task AtribuirTitular_DadosValidos_AtribuiELimpaFormatoDoCpf()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, notificacoes) = NovoService(lote);
        var comprador = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = comprador, StatusIngresso = EnumStatusIngresso.Pago };
        await ingressos.Adicionar(ingresso);

        await service.AtribuirTitularAsync(comprador, ingresso.Id, new AtribuirTitularIngressoDTO { Nome = "Fulano de Tal", Cpf = "123.456.789-00" });

        Assert.Equal("Fulano de Tal", ingresso.NomeTitular);
        Assert.Equal("12345678900", ingresso.CpfTitular);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == comprador);
    }

    // ─── ValidarIngressosNaPortariaAsync ─────────────────────────────────────

    [Fact]
    public async Task ValidarPortaria_CodigoInexistente_Rejeita()
    {
        var (service, _, _, _, _) = NovoService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ValidarIngressosNaPortariaAsync("CODIGO-INEXISTENTE"));
        Assert.Contains("inválido ou não encontrado", ex.Message);
    }

    [Fact]
    public async Task ValidarPortaria_JaUtilizado_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, StatusIngresso = EnumStatusIngresso.Utilizado, CodigoValidacao = "ABC123", DataUso = DateTime.UtcNow };
        await ingressos.Adicionar(ingresso);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ValidarIngressosNaPortariaAsync("ABC123"));
        Assert.Contains("já foi utilizado", ex.Message);
    }

    [Fact]
    public async Task ValidarPortaria_PagoSemTitular_Rejeita()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, StatusIngresso = EnumStatusIngresso.Pago, CodigoValidacao = "ABC123", NomeTitular = null };
        await ingressos.Adicionar(ingresso);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.ValidarIngressosNaPortariaAsync("ABC123"));
        Assert.Contains("pagamento e a atribuição do titular são obrigatórios", ex.Message);
    }

    [Fact]
    public async Task ValidarPortaria_PagoComTitular_MarcaUtilizadoUmaUnicaVez()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, notificacoes) = NovoService(lote);
        var usuarioId = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = usuarioId, StatusIngresso = EnumStatusIngresso.Pago, CodigoValidacao = "ABC123", NomeTitular = "Fulano" };
        await ingressos.Adicionar(ingresso);

        var primeira = await service.ValidarIngressosNaPortariaAsync("ABC123");
        Assert.True(primeira);
        Assert.Equal(EnumStatusIngresso.Utilizado, ingresso.StatusIngresso);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioId && n.Tipo == EnumTipoNotificacao.IngressoUtilizado);

        // Segunda tentativa com o mesmo codigo deve falhar (ja utilizado).
        var ex = await Assert.ThrowsAsync<Exception>(() => service.ValidarIngressosNaPortariaAsync("ABC123"));
        Assert.Contains("já foi utilizado", ex.Message);
    }

    // ─── ProcessarWebhookAsaasAsync ──────────────────────────────────────────

    [Theory]
    [InlineData("PAYMENT_OVERDUE")]
    [InlineData("PAYMENT_DELETED")]
    public async Task ProcessarWebhookAsaasAsync_EventoIrrelevante_IgnoraERetornaTrue(string evento)
    {
        var (service, _, _, _, _) = NovoService();

        Assert.True(await service.ProcessarWebhookAsaasAsync("pay_1", evento));
    }

    [Fact]
    public async Task ProcessarWebhookAsaasAsync_PagamentoNaoEncontrado_RetornaFalse()
    {
        var (service, _, _, _, _) = NovoService();

        Assert.False(await service.ProcessarWebhookAsaasAsync("pay_inexistente", "PAYMENT_CONFIRMED"));
    }

    [Fact]
    public async Task ProcessarWebhookAsaasAsync_IngressosPendentes_MarcaPagosEDecrementaLoteENotifica()
    {
        var lote = NovoLote(quantidadeDisponivel: 5);
        var (service, ingressos, lotes, _, notificacoes) = NovoService(lote);
        var usuarioId = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = usuarioId, AsaasPaymentId = "pay_1", StatusIngresso = EnumStatusIngresso.Pendente };
        await ingressos.Adicionar(ingresso);

        var resultado = await service.ProcessarWebhookAsaasAsync("pay_1", "PAYMENT_RECEIVED");

        Assert.True(resultado);
        Assert.Equal(EnumStatusIngresso.Pago, ingresso.StatusIngresso);
        Assert.Equal(4, (await lotes.ObterPorId(lote.Id))!.QuantidadeDisponivel);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuarioId && n.Tipo == EnumTipoNotificacao.IngressoConfirmado);
    }

    [Fact]
    public async Task ProcessarWebhookAsaasAsync_IngressosJaPagos_NaoNotificaNovamente()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, notificacoes) = NovoService(lote);
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = Guid.NewGuid(), AsaasPaymentId = "pay_1", StatusIngresso = EnumStatusIngresso.Pago };
        await ingressos.Adicionar(ingresso);

        var resultado = await service.ProcessarWebhookAsaasAsync("pay_1", "PAYMENT_CONFIRMED");

        Assert.True(resultado);
        Assert.Empty(notificacoes.Criadas);
    }

    // ─── ObterMeusIngressosAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ObterMeusIngressosAsync_SemIngressos_RetornaListaVazia()
    {
        var (service, _, _, _, _) = NovoService();

        Assert.Empty(await service.ObterMeusIngressosAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterMeusIngressosAsync_ComIngressoPago_RetornaDetalhesComQrCode()
    {
        var lote = NovoLote();
        var (service, ingressos, _, _, _) = NovoService(lote);
        var usuarioId = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = lote.Id, UsuarioId = usuarioId, StatusIngresso = EnumStatusIngresso.Pago, NomeTitular = "Fulano", CodigoValidacao = "COD1", PrecoPago = 50m };
        await ingressos.Adicionar(ingresso);

        var resultado = await service.ObterMeusIngressosAsync(usuarioId);

        Assert.Single(resultado);
        Assert.Equal("Fulano", resultado[0].NomeTitular);
        Assert.NotEmpty(resultado[0].QrCodeBase64);
    }

    [Fact]
    public async Task ObterMeusIngressosAsync_IngressoSemLote_RetornaPartidaDesconhecida()
    {
        var (service, ingressos, _, _, _) = NovoService();
        var usuarioId = Guid.NewGuid();
        var ingresso = new Ingresso { IngressoLoteId = Guid.NewGuid(), UsuarioId = usuarioId, StatusIngresso = EnumStatusIngresso.Pendente };
        await ingressos.Adicionar(ingresso);

        var resultado = await service.ObterMeusIngressosAsync(usuarioId);

        Assert.Single(resultado);
        Assert.Equal("Partida Desconhecida", resultado[0].NomePartida);
        Assert.Empty(resultado[0].QrCodeBase64); // pendente, ainda sem QR liberado
    }
}
