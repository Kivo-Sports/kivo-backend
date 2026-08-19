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

        public IngressoService(
            IRepositoryGenerics<Ingresso> ingressoRepo,
            IRepositoryGenerics<IngressoLote> loteRepo,
            IRepositoryGenerics<Partida> partidaRepo,
            IRepositoryGenerics<Time> timeRepo,
            IRepositoryGenerics<Usuario> usuarioRepo,
            IAsaasService asaasService)
        {
            _ingressoRepository = ingressoRepo;
            _loteRepository = loteRepo;
            _partidaRepository = partidaRepo;
            _timeRepository = timeRepo;
            _usuarioRepository = usuarioRepo;
            _asaasService = asaasService;
        }

        public async Task<List<IngressoDetalhesDTO>> ComprarIngressosAsync(Guid usuarioId, RealizarCompraDTO compraDTO)
        {
            var usuario = await _usuarioRepository.ObterPorId(usuarioId);
            if (usuario == null)
                throw new Exception("Usuário não encontrado.");

            var lote = await _loteRepository.ObterPorId(compraDTO.IngressoLoteId);
            if (lote == null)
                throw new Exception("Lote de ingressos não encontrado.");

            if (!lote.Ativo)
                throw new Exception("Este lote de ingressos não está mais ativo.");

            if (lote.QuantidadeDisponivel < compraDTO.Quantidade)
                throw new Exception($"Estoque insuficiente. Quantidade disponível: {lote.QuantidadeDisponivel}");

            var (nomePartida, dataPartida, localPartida) = await ObterDadosPartidaAsync(lote.PartidaId);

            var customerId = await _asaasService.ObterOuCriarClienteAsync(
                usuario.Nome,
                usuario.Cpf ?? "00000000000",
                usuario.Email
            );

            var ingressosGerados = new List<IngressoDetalhesDTO>();

            for (int i = 0; i < compraDTO.Quantidade; i++)
            {
                var ingressoId = Guid.NewGuid();

                var cobranca = await _asaasService.CriarCobrancaPixAsync(
                    customerId,
                    lote.Preco,
                    $"Ingresso Kivo - {nomePartida} ({lote.NomeLote})",
                    ingressoId.ToString()
                );

                var dadosPix = await _asaasService.ObterQrCodePixAsync(cobranca.Id);

                var novoIngresso = new Ingresso
                {
                    Id = ingressoId,
                    IngressoLoteId = lote.Id,
                    UsuarioId = usuarioId,
                    PrecoPago = lote.Preco,
                    DataCompra = DateTime.UtcNow,
                    StatusIngresso = EnumStatusIngresso.Pendente,
                    CodigoValidacao = Guid.NewGuid().ToString("N").ToUpper(),
                    AsaasPaymentId = cobranca.Id
                };

                await _ingressoRepository.Adicionar(novoIngresso);

                var dto = MapearParaDto(novoIngresso, lote.NomeLote, nomePartida, dataPartida, localPartida);
                dto.PixCopiaCola = dadosPix.Payload;
                dto.QrCodeBase64 = $"data:image/png;base64,{dadosPix.EncodedImage}";
                ingressosGerados.Add(dto);
            }

            return ingressosGerados;
        }

        public async Task<bool> ProcessarWebhookAsaasAsync(string asaasPaymentId, string evento)
        {
            if (evento != "PAYMENT_RECEIVED" && evento != "PAYMENT_CONFIRMED")
                return true;

            var ingressos = await _ingressoRepository.Buscar(i => i.AsaasPaymentId == asaasPaymentId);
            var ingresso = ingressos.FirstOrDefault();

            if (ingresso == null)
                return false;

            if (ingresso.StatusIngresso != EnumStatusIngresso.Pago && ingresso.StatusIngresso != EnumStatusIngresso.Utilizado)
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

            ingresso.StatusIngresso = EnumStatusIngresso.Pago;
            await _ingressoRepository.Atualizar(ingresso);

            var lote = await _loteRepository.ObterPorId(ingresso.IngressoLoteId);
            if (lote != null && lote.QuantidadeDisponivel > 0)
            {
                lote.QuantidadeDisponivel -= 1;
                await _loteRepository.Atualizar(lote);
            }

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

        public async Task<bool> ValidarIngressosNaPortariaAsync(string codigoValidacao)
        {
            var ingressos = await _ingressoRepository.Buscar(i => i.CodigoValidacao == codigoValidacao);
            var ingresso = ingressos.FirstOrDefault();

            if (ingresso == null)
                throw new Exception("Ingresso inválido ou não encontrado.");

            if (ingresso.StatusIngresso == EnumStatusIngresso.Utilizado)
                throw new Exception($"Este ingresso já foi utilizado em {ingresso.DataUso:dd/MM/yyyy HH:mm}.");

            if (ingresso.StatusIngresso != EnumStatusIngresso.Pago)
                throw new Exception("Este ingresso não está válido para entrada. O pagamento precisa ser confirmado.");

            ingresso.StatusIngresso = EnumStatusIngresso.Utilizado;
            ingresso.DataUso = DateTime.UtcNow;

            await _ingressoRepository.Atualizar(ingresso);
            return true;
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
            bool estaPago = ingresso.StatusIngresso == EnumStatusIngresso.Pago || ingresso.StatusIngresso == EnumStatusIngresso.Utilizado;

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
                CodigoValidacao = estaPago ? ingresso.CodigoValidacao : string.Empty,
                QrCodeBase64 = estaPago ? GerarQrCodeBase64(ingresso.CodigoValidacao) : string.Empty,
                PixCopiaCola = string.Empty
            };
        }

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