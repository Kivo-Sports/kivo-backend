using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using QRCoder;

namespace kivoBackend.Application.Services
{
    public class IngressoService : IIngressoService
    {
        private readonly IRepositoryGenerics<Ingresso> _ingressoRepository;
        private readonly IRepositoryGenerics<IngressoLote> _loteRepository;
        private readonly IRepositoryGenerics<Partida> _partidaRepository;
        private readonly IRepositoryGenerics<Time> _timeRepository;
        private readonly IRepositoryGenerics<Usuario> _usuarioRepository;
        private readonly IAsaasService _asaasService;
        private readonly INotificacaoService _notificacaoService;

        public IngressoService(
            IRepositoryGenerics<Ingresso> ingressoRepo,
            IRepositoryGenerics<IngressoLote> loteRepo,
            IRepositoryGenerics<Partida> partidaRepo,
            IRepositoryGenerics<Time> timeRepo,
            IRepositoryGenerics<Usuario> usuarioRepo,
            IAsaasService asaasService,
            INotificacaoService notificacaoService)
        {
            _ingressoRepository = ingressoRepo;
            _loteRepository = loteRepo;
            _partidaRepository = partidaRepo;
            _timeRepository = timeRepo;
            _usuarioRepository = usuarioRepo;
            _asaasService = asaasService;
            _notificacaoService = notificacaoService;
        }

        public async Task<CompraIngressosResponseDTO> ComprarIngressosAsync(Guid usuarioId, RealizarCompraDTO compraDTO)
        {
            var usuario = await _usuarioRepository.ObterPorId(usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado.");

            var itensSolicitados = NormalizarItensCompra(compraDTO);
            var itens = new List<(IngressoLote Lote, int Quantidade)>();

            foreach (var item in itensSolicitados)
            {
                var lote = await _loteRepository.ObterPorId(item.IngressoLoteId);
                if (lote == null)
                    throw new Exception("Lote de ingressos não encontrado.");
                if (!lote.Ativo)
                    throw new Exception($"O lote '{lote.NomeLote}' não está mais ativo.");
                if (lote.QuantidadeDisponivel < item.Quantidade)
                    throw new Exception($"Estoque insuficiente no lote '{lote.NomeLote}'. Quantidade disponível: {lote.QuantidadeDisponivel}");

                itens.Add((lote, item.Quantidade));
            }

            var valorTotal = itens.Sum(item => item.Lote.Preco * item.Quantidade);
            var descricao = $"Ingressos Kivo - {string.Join(", ", itens.Select(i => i.Lote.NomeLote))}";

            var customerId = await _asaasService.ObterOuCriarClienteAsync(
                usuario.Nome,
                usuario.Cpf ?? "00000000000",
                usuario.Email
            );

            // Uma cobrança representa o carrinho inteiro. Todos os ingressos guardam o mesmo id.
            var cobranca = await _asaasService.CriarCobrancaPixAsync(
                customerId, valorTotal, descricao, Guid.NewGuid().ToString());
            var dadosPix = await _asaasService.ObterQrCodePixAsync(cobranca.Id);
            var ingressosGerados = new List<IngressoDetalhesDTO>();

            foreach (var (lote, quantidade) in itens)
            {
                var (nomePartida, dataPartida, localPartida) = await ObterDadosPartidaAsync(lote.PartidaId);
                for (int i = 0; i < quantidade; i++)
                {
                    var novoIngresso = new Ingresso
                    {
                        Id = Guid.NewGuid(),
                        IngressoLoteId = lote.Id,
                        UsuarioId = usuarioId,
                        PrecoPago = lote.Preco,
                        DataCompra = DateTime.UtcNow,
                        StatusIngresso = EnumStatusIngresso.Pendente,
                        CodigoValidacao = Guid.NewGuid().ToString("N").ToUpper(),
                        AsaasPaymentId = cobranca.Id
                    };

                    await _ingressoRepository.Adicionar(novoIngresso);
                    ingressosGerados.Add(MapearParaDto(novoIngresso, lote.NomeLote, nomePartida, dataPartida, localPartida));
                }
            }

            return new CompraIngressosResponseDTO
            {
                AsaasPaymentId = cobranca.Id,
                ValorTotal = valorTotal,
                PixCopiaCola = dadosPix.Payload,
                QrCodeBase64 = $"data:image/png;base64,{dadosPix.EncodedImage}",
                Ingressos = ingressosGerados
            };
        }

        public async Task<bool> ProcessarWebhookAsaasAsync(string asaasPaymentId, string evento)
        {
            if (evento != "PAYMENT_RECEIVED" && evento != "PAYMENT_CONFIRMED")
                return true;

            var ingressos = await _ingressoRepository.Buscar(i => i.AsaasPaymentId == asaasPaymentId);
            var ingressosPendentes = ingressos
                .Where(i => i.StatusIngresso != EnumStatusIngresso.Pago && i.StatusIngresso != EnumStatusIngresso.Utilizado)
                .ToList();

            if (!ingressos.Any())
                return false;

            foreach (var ingresso in ingressosPendentes)
            {
                ingresso.StatusIngresso = EnumStatusIngresso.Pago;
                await _ingressoRepository.Atualizar(ingresso);

                var lote = await _loteRepository.ObterPorId(ingresso.IngressoLoteId);
                if (lote != null && lote.QuantidadeDisponivel > 0)
                {
                    lote.QuantidadeDisponivel -= 1;
                    await _loteRepository.Atualizar(lote);
                }

            }

            if (ingressosPendentes.Any())
                await NotificarPagamentoConfirmadoAsync(ingressosPendentes);

            return true;
        }

        public async Task<bool> ConfirmarPagamentoAsync(Guid ingressoId)
        {
            var ingresso = await _ingressoRepository.ObterPorId(ingressoId);
            if (ingresso == null)
                throw new Exception("Ingresso não encontrado.");

            if (ingresso.StatusIngresso == EnumStatusIngresso.Pago)
                throw new Exception("Este ingresso já possui o pagamento confirmado.");

            if (ingresso.StatusIngresso == EnumStatusIngresso.Utilizado)
                throw new Exception("Este ingresso já foi utilizado.");

            if (!string.IsNullOrWhiteSpace(ingresso.AsaasPaymentId))
            {
                var statusAsaas = await _asaasService.ConsultarStatusCobrancaAsync(ingresso.AsaasPaymentId);

                bool estaPagoNoAsaas = statusAsaas == "RECEIVED" || statusAsaas == "CONFIRMED" || statusAsaas == "DONE";

                if (!estaPagoNoAsaas)
                {
                    throw new InvalidOperationException($"O pagamento deste ingresso ainda não foi confirmado no Asaas (Status atual: {statusAsaas}). O ingresso permanece Pendente.");
                }
            }

            var ingressosDaCompra = string.IsNullOrWhiteSpace(ingresso.AsaasPaymentId)
                ? new List<Ingresso> { ingresso }
                : (await _ingressoRepository.Buscar(i => i.AsaasPaymentId == ingresso.AsaasPaymentId)).ToList();
            var ingressosPendentes = ingressosDaCompra.Where(i => i.StatusIngresso == EnumStatusIngresso.Pendente).ToList();

            foreach (var ingressoPendente in ingressosPendentes)
            {
                ingressoPendente.StatusIngresso = EnumStatusIngresso.Pago;
                await _ingressoRepository.Atualizar(ingressoPendente);
                var lote = await _loteRepository.ObterPorId(ingressoPendente.IngressoLoteId);
                if (lote != null && lote.QuantidadeDisponivel > 0)
                {
                    lote.QuantidadeDisponivel -= 1;
                    await _loteRepository.Atualizar(lote);
                }
            }

            if (ingressosPendentes.Any())
                await NotificarPagamentoConfirmadoAsync(ingressosPendentes);

            return true;
        }

        public async Task<List<IngressoDetalhesDTO>> ObterMeusIngressosAsync(Guid usuarioId)
        {
            var ingressos = await _ingressoRepository.Buscar(i => i.UsuarioId == usuarioId);
            var dtos = new List<IngressoDetalhesDTO>();

            foreach (var ingresso in ingressos)
            {
                var lote = await _loteRepository.ObterPorId(ingresso.IngressoLoteId);
                var (nomePartida, dataPartida, localPartida) = lote != null
                    ? await ObterDadosPartidaAsync(lote.PartidaId)
                    : ("Partida Desconhecida", DateTime.MinValue, "Local não informado");

                dtos.Add(MapearParaDto(ingresso, lote?.NomeLote ?? "Lote", nomePartida, dataPartida, localPartida));
            }

            return dtos;
        }

        public async Task AtribuirTitularAsync(Guid compradorId, Guid ingressoId, AtribuirTitularIngressoDTO dto)
        {
            var ingresso = await _ingressoRepository.ObterPorId(ingressoId);
            if (ingresso == null)
                throw new Exception("Ingresso não encontrado.");
            if (ingresso.UsuarioId != compradorId)
                throw new UnauthorizedAccessException("Apenas o comprador pode atribuir o titular deste ingresso.");
            if (ingresso.StatusIngresso != EnumStatusIngresso.Pago)
                throw new InvalidOperationException("O titular só pode ser atribuído após a confirmação do pagamento.");
            if (!string.IsNullOrWhiteSpace(ingresso.NomeTitular))
                throw new InvalidOperationException("Este ingresso já possui um titular e não pode mais ser alterado.");

            var cpfLimpo = LimparCpf(dto.Cpf);
            if (cpfLimpo.Length != 11)
                throw new Exception("Informe um CPF válido.");
            if (string.IsNullOrWhiteSpace(dto.Nome))
                throw new Exception("Informe o nome do titular.");

            var ingressosDaCompra = string.IsNullOrWhiteSpace(ingresso.AsaasPaymentId)
                ? new List<Ingresso> { ingresso }
                : (await _ingressoRepository.Buscar(i => i.AsaasPaymentId == ingresso.AsaasPaymentId)).ToList();

            if (ingressosDaCompra.Any(i => i.Id != ingresso.Id && LimparCpf(i.CpfTitular) == cpfLimpo))
                throw new InvalidOperationException("Este CPF já foi atribuído a outro ingresso desta compra.");

            ingresso.NomeTitular = dto.Nome.Trim();
            ingresso.CpfTitular = cpfLimpo;
            await _ingressoRepository.Atualizar(ingresso);

            await _notificacaoService.CriarNotificacaoAsync(
                compradorId,
                "Titular vinculado ao ingresso 🎟️",
                $"{ingresso.NomeTitular} foi vinculado com sucesso a um dos seus ingressos.",
                EnumTipoNotificacao.IngressoConfirmado,
                link: "/meus-ingressos",
                enviarEmail: false
            );
        }

        public async Task<bool> ValidarIngressosNaPortariaAsync(string codigoValidacao)
        {
            var ingressos = await _ingressoRepository.Buscar(i => i.CodigoValidacao == codigoValidacao);
            var ingresso = ingressos.FirstOrDefault();

            if (ingresso == null)
                throw new Exception("Ingresso inválido ou não encontrado.");

            if (ingresso.StatusIngresso == EnumStatusIngresso.Utilizado)
                throw new Exception($"Este ingresso já foi utilizado em {ingresso.DataUso:dd/MM/yyyy HH:mm}.");

            if (ingresso.StatusIngresso != EnumStatusIngresso.Pago || string.IsNullOrWhiteSpace(ingresso.NomeTitular))
                throw new Exception("Este ingresso não está válido para entrada. O pagamento e a atribuição do titular são obrigatórios.");

            ingresso.StatusIngresso = EnumStatusIngresso.Utilizado;
            ingresso.DataUso = DateTime.UtcNow;

            await _ingressoRepository.Atualizar(ingresso);

            await _notificacaoService.CriarNotificacaoAsync(
                ingresso.UsuarioId,
                "Ingresso Validado na Entrada ✅",
                $"Seu ingresso acabou de ser utilizado para entrada no evento em {ingresso.DataUso:dd/MM/yyyy HH:mm}.",
                EnumTipoNotificacao.IngressoUtilizado,
                link: null,
                enviarEmail: false
            );

            return true;
        }

        private static List<ItemCompraIngressoDTO> NormalizarItensCompra(RealizarCompraDTO compraDTO)
        {
            var itens = compraDTO.Itens?.Where(i => i.IngressoLoteId != Guid.Empty).ToList()
                ?? new List<ItemCompraIngressoDTO>();

            if (!itens.Any())
                throw new Exception("Informe pelo menos um lote de ingressos.");

            var itensAgrupados = itens
                .GroupBy(i => i.IngressoLoteId)
                .Select(g => new ItemCompraIngressoDTO
                {
                    IngressoLoteId = g.Key,
                    Quantidade = g.Sum(i => i.Quantidade)
                })
                .ToList();

            if (itensAgrupados.Any(i => i.Quantidade < 1))
                throw new Exception("A quantidade de cada lote deve ser maior que zero.");

            if (itensAgrupados.Sum(i => i.Quantidade) > 10)
                throw new Exception("A quantidade máxima por compra é de 10 ingressos.");

            return itensAgrupados;
        }

        private async Task NotificarPagamentoConfirmadoAsync(IEnumerable<Ingresso> ingressos)
        {
            var listaIngressos = ingressos.ToList();
            var primeiroIngresso = listaIngressos.First();
            var primeiroLote = await _loteRepository.ObterPorId(primeiroIngresso.IngressoLoteId);
            var (nomePartida, _, _) = await ObterDadosPartidaAsync(primeiroLote?.PartidaId);
            var quantidade = listaIngressos.Count;
            var titulo = quantidade == 1 ? "Pagamento Confirmado! 🎟️" : "Pagamentos Confirmados! 🎟️";
            var mensagem = quantidade == 1
                ? $"Seu ingresso para {nomePartida} foi pago. Atribua um titular para liberar o QR Code de entrada."
                : $"Seus {quantidade} ingressos para {nomePartida} foram pagos. Atribua os titulares para liberar os QR Codes de entrada.";

            await _notificacaoService.CriarNotificacaoAsync(
                primeiroIngresso.UsuarioId,
                titulo,
                mensagem,
                EnumTipoNotificacao.IngressoConfirmado,
                link: "/meus-ingressos",
                enviarEmail: true
            );
        }

        private async Task<(string NomePartida, DateTime DataPartida, string LocalPartida)> ObterDadosPartidaAsync(Guid? partidaId)
        {
            if (!partidaId.HasValue || partidaId.Value == Guid.Empty)
                return ("Partida não vinculada ao lote", DateTime.MinValue, "Local não informado");

            var partida = await _partidaRepository.ObterPorId(partidaId.Value);
            if (partida == null)
                return ("Partida não encontrada", DateTime.MinValue, "Local não encontrado");

            var timeCasa = partida.TimeCasaId.HasValue && partida.TimeCasaId.Value != Guid.Empty
                ? await _timeRepository.ObterPorId(partida.TimeCasaId.Value)
                : null;

            var timeVisitante = partida.TimeVisitanteId.HasValue && partida.TimeVisitanteId.Value != Guid.Empty
                ? await _timeRepository.ObterPorId(partida.TimeVisitanteId.Value)
                : null;

            string nomeCasa = !string.IsNullOrWhiteSpace(timeCasa?.Nome) ? timeCasa.Nome : "Time Casa";
            string nomeVisitante = !string.IsNullOrWhiteSpace(timeVisitante?.Nome) ? timeVisitante.Nome : "Time Visitante";

            string nomeConfronto = (timeCasa != null || timeVisitante != null)
                ? $"{nomeCasa} x {nomeVisitante}"
                : "Confronto a definir";

            string local = !string.IsNullOrWhiteSpace(partida.Local) ? partida.Local : "Local a definir";

            return (nomeConfronto, partida.DataHora ?? DateTime.MinValue, local);
        }

        private IngressoDetalhesDTO MapearParaDto(Ingresso ingresso, string nomeLote, string nomePartida, DateTime dataPartida, string localPartida)
        {
            bool qrCodeLiberado = (ingresso.StatusIngresso == EnumStatusIngresso.Pago || ingresso.StatusIngresso == EnumStatusIngresso.Utilizado)
                && !string.IsNullOrWhiteSpace(ingresso.NomeTitular);

            return new IngressoDetalhesDTO
            {
                Id = ingresso.Id,
                NomeLote = nomeLote,
                NomePartida = nomePartida,
                DataPartida = dataPartida,
                LocalPartida = localPartida,
                PrecoPago = ingresso.PrecoPago,
                DataCompra = ingresso.DataCompra,
                Status = ingresso.StatusIngresso,
                CodigoValidacao = qrCodeLiberado ? ingresso.CodigoValidacao : string.Empty,
                QrCodeBase64 = qrCodeLiberado ? GerarQrCodeBase64(ingresso.CodigoValidacao) : string.Empty,
                PixCopiaCola = string.Empty,
                NomeTitular = ingresso.NomeTitular ?? string.Empty,
                CpfTitular = ingresso.CpfTitular ?? string.Empty
            };
        }

        private static string LimparCpf(string? cpf) => new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());


        private string GerarQrCodeBase64(string texto)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);
                return $"data:image/png;base64,{Convert.ToBase64String(qrCodeAsPngByteArr)}";
            }
        }
    }
}
