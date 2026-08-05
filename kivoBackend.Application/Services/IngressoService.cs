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

        public IngressoService(
            IRepositoryGenerics<Ingresso> ingressoRepo,
            IRepositoryGenerics<IngressoLote> loteRepo,
            IRepositoryGenerics<Partida> partidaRepo,
            IRepositoryGenerics<Time> timeRepo)
        {
            _ingressoRepository = ingressoRepo;
            _loteRepository = loteRepo;
            _partidaRepository = partidaRepo;
            _timeRepository = timeRepo;
        }

        public async Task<List<IngressoDetalhesDTO>> ComprarIngressosAsync(Guid usuarioId, RealizarCompraDTO compraDTO)
        {
            var lote = await _loteRepository.ObterPorId(compraDTO.IngressoLoteId);
            if (lote == null)
            {
                throw new Exception("Lote de ingressos não encontrado.");
            }
            if (!lote.Ativo)
            {
                throw new Exception("Este lote de ingressos não está mais ativo.");
            }
            if (lote.QuantidadeDisponivel < compraDTO.Quantidade)
            {
                throw new Exception($"Estoque insuficiente. Quantidade disponível: {lote.QuantidadeDisponivel}");
            }

            lote.QuantidadeDisponivel -= compraDTO.Quantidade;
            await _loteRepository.Atualizar(lote);

            var ingressosGerados = new List<Ingresso>();
            for (int i = 0; i < compraDTO.Quantidade; i++)
            {
                var novoIngresso = new Ingresso
                {
                    IngressoLoteId = lote.Id,
                    UsuarioId = usuarioId,
                    PrecoPago = lote.Preco,
                    DataCompra = DateTime.UtcNow,
                    StatusIngresso = EnumStatusIngresso.Pendente,
                    CodigoValidacao = Guid.NewGuid().ToString("N").ToUpper()
                };

                await _ingressoRepository.Adicionar(novoIngresso);
                ingressosGerados.Add(novoIngresso);
            }

            var (nomePartida, dataPartida, localPartida) = await ObterDadosPartidaAsync(lote.PartidaId);

            return ingressosGerados.Select(i => MapearParaDto(i, lote.NomeLote, nomePartida, dataPartida, localPartida)).ToList();
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
                throw new Exception("Este ingresso não está válido para entrada.");

            ingresso.StatusIngresso = EnumStatusIngresso.Utilizado;
            ingresso.DataUso = DateTime.UtcNow;

            await _ingressoRepository.Atualizar(ingresso);
            return true;
        }

        private async Task<(string NomePartida, DateTime DataPartida, string LocalPartida)> ObterDadosPartidaAsync(Guid? partidaId)
        {
            if (!partidaId.HasValue || partidaId.Value == Guid.Empty)
                return (
                    "Partida não vinculada ao lote",
                    DateTime.MinValue,
                    "Local não informado"
                );

            var partida = await _partidaRepository.ObterPorId(partidaId.Value);

            if (partida == null)
                return (
                    "Partida não encontrada",
                    DateTime.MinValue,
                    "Local não encontrado"
                );

            var timeCasa = partida.TimeCasaId.HasValue && partida.TimeCasaId.Value != Guid.Empty
                ? await _timeRepository.ObterPorId(partida.TimeCasaId.Value)
                : null;

            var timeVisitante = partida.TimeVisitanteId.HasValue && partida.TimeVisitanteId.Value != Guid.Empty
                ? await _timeRepository.ObterPorId(partida.TimeVisitanteId.Value)
                : null;

            string nomeCasa = !string.IsNullOrWhiteSpace(timeCasa?.Nome)
                ? timeCasa.Nome
                : "Time Casa";

            string nomeVisitante = !string.IsNullOrWhiteSpace(timeVisitante?.Nome)
                ? timeVisitante.Nome
                : "Time Visitante";

            string nomeConfronto = (timeCasa != null || timeVisitante != null)
                ? $"{nomeCasa} x {nomeVisitante}"
                : "Confronto a definir";

            string local = !string.IsNullOrWhiteSpace(partida.Local)
                ? partida.Local
                : "Local a definir";

            return (
                nomeConfronto,
                partida.DataHora ?? DateTime.MinValue,
                local
            );
        }

        private IngressoDetalhesDTO MapearParaDto(Ingresso ingresso, string nomeLote, string nomePartida, DateTime dataPartida, string localPartida)
        {
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
                CodigoValidacao = ingresso.CodigoValidacao,
                QrCodeBase64 = GerarQrCodeBase64(ingresso.CodigoValidacao)
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