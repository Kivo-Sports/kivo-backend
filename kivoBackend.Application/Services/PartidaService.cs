using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class PartidaService : ServiceGenerics<Partida>, IPartidaService
    {
        private readonly IRepositoryGenerics<Partida> _repositoryGenerics;
        private readonly IRepositoryCampeonato _repositoryCampeonato;
        public PartidaService(IRepositoryGenerics<Partida> repositoryGenerics, IRepositoryCampeonato repositoryCampeonato) : base(repositoryGenerics)
        {
            _repositoryGenerics = repositoryGenerics;
            _repositoryCampeonato = repositoryCampeonato;
        }
        public async Task GerarTabela(Guid campeonatoId)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);
            if (campeonato == null) throw new Exception("Campeonato não encontrado.");

            var times = campeonato.CampeonatoTimes
                .Where(ct => ct.EnumStatusParticipacao == EnumStatusParticipacao.Aceito)
                .Select(ct => ct.TimeId)
                .ToList();

            if (times.Count < 2)
                throw new Exception("É necessário pelo menos 2 times aceitos para gerar os jogos.");

            switch (campeonato.FormatoCampeonato)
            {
                case EnumFormatoCampeonato.PontosCorridos:
                    await GerarPontosCorridos(campeonatoId, times);
                    break;

                case EnumFormatoCampeonato.MataMata:
                    if ((times.Count & (times.Count - 1)) != 0)
                        throw new Exception("Para o formato Mata-Mata direto, o número de times deve ser potência de 2 (2, 4, 8, 16...).");

                    await GerarMataMataInicial(campeonatoId, times);
                    break;

                case EnumFormatoCampeonato.Hibrido:
                    await GerarPontosCorridos(campeonatoId, times);
                    break;

                default:
                    throw new Exception("Formato de campeonato inválido.");
            }
        }

        public async Task GerarPontosCorridos(Guid campeonatoId, List<Guid> times)
        {
            if (times.Count % 2 != 0) times.Add(Guid.Empty);

            int numTimes = times.Count;
            int numRodadas = numTimes - 1;

            for (int r = 0; r < numRodadas; r++)
            {
                for (int j = 0; j < numTimes / 2; j++)
                {
                    var casa = times[j];
                    var fora = times[numTimes - 1 - j];

                    if (casa != Guid.Empty && fora != Guid.Empty)
                    {
                        await _repositoryGenerics.Adicionar(new Partida
                        {
                            CampeonatoId = campeonatoId,
                            TimeCasaId = casa,
                            TimeVisitanteId = fora,
                            Rodada = r + 1
                        });
                    }
                }
                var ultimo = times[numTimes - 1];
                times.RemoveAt(numTimes - 1);
                times.Insert(1, ultimo);
            }
        }

        public async Task GerarMataMataInicial(Guid campeonatoId, List<Guid> times)
        {
            var random = new Random();
            var timesSorteados = times.OrderBy(x => random.Next()).ToList();

            int qtdTimes = timesSorteados.Count;
            EnumFaseMataMata faseInicial;

            if (qtdTimes <= 2) faseInicial = EnumFaseMataMata.Final;
            else if (qtdTimes <= 4) faseInicial = EnumFaseMataMata.Semifinais;
            else if (qtdTimes <= 8) faseInicial = EnumFaseMataMata.Quartas;
            else faseInicial = EnumFaseMataMata.Oitavas;

            for (int i = 0; i < qtdTimes; i += 2)
            {
                await _repositoryGenerics.Adicionar(new Partida
                {
                    CampeonatoId = campeonatoId,
                    TimeCasaId = timesSorteados[i],
                    TimeVisitanteId = timesSorteados[i + 1],
                    Fase = faseInicial,
                    NumeroJogoChave = (i / 2) + 1
                });
            }
        }
        public async Task AtualizarPlacarMataMata(Partida partidaFinalizada)
        {
            Guid vencedorId = partidaFinalizada.GolsTimeCasa > partidaFinalizada.GolsTimeVisitante
                ? partidaFinalizada.TimeCasaId.Value
                : partidaFinalizada.TimeVisitanteId.Value;

            if (partidaFinalizada.Fase == EnumFaseMataMata.Final) return;

            int numeroJogoProximaFase = (partidaFinalizada.NumeroJogoChave + 1) / 2;
            EnumFaseMataMata proximaFase = (EnumFaseMataMata)((int)partidaFinalizada.Fase + 1);

            var jogoParceiro = await _repositoryGenerics.BuscarPrimeiro(p =>
                p.CampeonatoId == partidaFinalizada.CampeonatoId &&
                p.Fase == partidaFinalizada.Fase &&
                p.NumeroJogoChave != partidaFinalizada.NumeroJogoChave &&
                ((p.NumeroJogoChave + 1) / 2) == numeroJogoProximaFase);

            if (jogoParceiro != null && jogoParceiro.Finalizado)
            {
                Guid vencedorParceiroId = jogoParceiro.GolsTimeCasa > jogoParceiro.GolsTimeVisitante
                    ? jogoParceiro.TimeCasaId.Value
                    : jogoParceiro.TimeVisitanteId.Value;

                await _repositoryGenerics.Adicionar(new Partida
                {
                    CampeonatoId = partidaFinalizada.CampeonatoId,
                    TimeCasaId = vencedorId,
                    TimeVisitanteId = vencedorParceiroId,
                    Fase = proximaFase,
                    NumeroJogoChave = numeroJogoProximaFase
                });
            }
        }
        public async Task<List<CriterioDesempateDTO>> ObterClassificacaoProximaFase(Guid campeonatoId)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);
            var partidas = await _repositoryGenerics.Buscar(p => p.CampeonatoId == campeonatoId && p.Rodada != null && p.Finalizado);

            var tabela = new Dictionary<Guid, CriterioDesempateDTO>();
            var times = campeonato.CampeonatoTimes.Where(ct => ct.EnumStatusParticipacao == EnumStatusParticipacao.Aceito);

            foreach (var time in times)
            {
                tabela[time.TimeId] = new CriterioDesempateDTO { TimeId = time.TimeId };
            }

            foreach (var p in partidas)
            {
                var casa = tabela[p.TimeCasaId.Value];
                var fora = tabela[p.TimeVisitanteId.Value];

                casa.GolsFeitos += p.GolsTimeCasa;
                fora.GolsFeitos += p.GolsTimeVisitante;
                casa.SaldoGols += (p.GolsTimeCasa - p.GolsTimeVisitante);
                fora.SaldoGols += (p.GolsTimeVisitante - p.GolsTimeCasa);

                if (p.GolsTimeCasa > p.GolsTimeVisitante)
                {
                    casa.Pontos += campeonato.PontosVitoria ?? 0;
                    casa.Vitorias++;
                    fora.Pontos += campeonato.PontosDerrota ?? 0;
                }
                else if (p.GolsTimeCasa < p.GolsTimeVisitante)
                {
                    fora.Pontos += campeonato.PontosVitoria ?? 0;
                    fora.Vitorias++;
                    casa.Pontos += campeonato.PontosDerrota ?? 0;
                }
                else
                {
                    casa.Pontos += campeonato.PontosEmpate ?? 0;
                    fora.Pontos += campeonato.PontosEmpate ?? 0;
                }
            }

            return tabela.Values
                .OrderByDescending(x => x.Pontos)
                .ThenByDescending(x => x.Vitorias)
                .ThenByDescending(x => x.SaldoGols)
                .ToList();
        }
        public async Task VerificarFimFasePontosCorridos(Guid campeonatoId)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);

            if (campeonato.FormatoCampeonato != EnumFormatoCampeonato.Hibrido) return;

            var totalPartidasPC = await _repositoryGenerics.Buscar(p => p.CampeonatoId == campeonatoId && p.Rodada != null);

            if (totalPartidasPC.All(p => p.Finalizado))
            {
                var classificacao = await ObterClassificacaoProximaFase(campeonatoId);
                int qtdCorte = campeonato.QuantidadeTimesClassificam ?? 0;

                if (qtdCorte == 0) throw new Exception("Configuração de classificados inválida para campeonato Híbrido.");

                var classificados = classificacao
                    .Take(qtdCorte)
                    .Select(x => x.TimeId)
                    .ToList();

                await GerarMataMataInicial(campeonatoId, classificados);
            }
        }

        public async Task<IEnumerable<ChaveamentoDTO>> ObterChaveamentoMataMata(Guid campeonatoId)
        {
            var todas = await _repositoryGenerics.ObterComIncludes(
                p => p.TimeCasa,
                p => p.TimeVisitante
            );

            var partidas = todas.Where(p => p.CampeonatoId == campeonatoId && p.Fase != EnumFaseMataMata.Nenhuma);

            return partidas
                .GroupBy(p => p.Fase)
                .OrderBy(g => (int)g.Key)
                .Select(g => new ChaveamentoDTO
                {
                    Fase = g.Key.ToString(),
                    Partidas = g.Select(p => new TabelaClassificacaoMataMataDTO
                    {
                        Id = p.Id,
                        NumeroJogoChave = p.NumeroJogoChave,
                        TimeCasa = p.TimeCasa?.Nome ?? "A definir",
                        TimeVisitante = p.TimeVisitante?.Nome ?? "A definir",
                        LogoCasa = p.TimeCasa?.LogoUrl,
                        LogoVisitante = p.TimeVisitante?.LogoUrl,
                        GolsCasa = p.GolsTimeCasa,
                        GolsVisitante = p.GolsTimeVisitante,
                        Finalizado = p.Finalizado
                    }).ToList()
                });
        }

        public async Task<List<TabelaClassificacaoDTO>> ObterClassificacaoTabela(Guid campeonatoId)
        {
            var campeonato = await _repositoryCampeonato.ObterCampeonatoPorId(campeonatoId);
            if (campeonato == null) throw new Exception("Campeonato não encontrado.");

            var todasPartidas = await _repositoryGenerics.ObterComIncludes(
                p => p.TimeCasa,
                p => p.TimeVisitante
            );

            var partidas = todasPartidas.Where(p =>
                p.CampeonatoId == campeonatoId &&
                p.Rodada != null &&
                p.Finalizado
            );

            var tabelaDict = new Dictionary<Guid, TabelaClassificacaoDTO>();

            foreach (var vinculo in campeonato.CampeonatoTimes.Where(v => v.EnumStatusParticipacao == EnumStatusParticipacao.Aceito))
            {
                tabelaDict[vinculo.TimeId] = new TabelaClassificacaoDTO
                {
                    TimeId = vinculo.TimeId,
                    NomeTime = vinculo.Time?.Nome ?? "Time Desconhecido",
                    LogoUrl = vinculo.Time?.LogoUrl
                };
            }

            foreach (var p in partidas)
            {
                var casa = tabelaDict[p.TimeCasaId.Value];
                var fora = tabelaDict[p.TimeVisitanteId.Value];

                casa.Jogos++;
                fora.Jogos++;
                casa.GolsFeitos += p.GolsTimeCasa;
                casa.GolsSofridos += p.GolsTimeVisitante;
                fora.GolsFeitos += p.GolsTimeVisitante;
                fora.GolsSofridos += p.GolsTimeCasa;

                if (p.GolsTimeCasa > p.GolsTimeVisitante)
                {
                    casa.Pontos += campeonato.PontosVitoria ?? 0;
                    casa.Vitorias++;
                    fora.Pontos += campeonato.PontosDerrota ?? 0;
                    fora.Derrotas++;
                }
                else if (p.GolsTimeCasa < p.GolsTimeVisitante)
                {
                    fora.Pontos += campeonato.PontosVitoria ?? 0;
                    fora.Vitorias++;
                    casa.Pontos += campeonato.PontosDerrota ?? 0;
                    casa.Derrotas++;
                }
                else
                {
                    casa.Pontos += campeonato.PontosEmpate ?? 0;
                    fora.Pontos += campeonato.PontosEmpate ?? 0;
                    casa.Empates++;
                    fora.Empates++;
                }

                casa.SaldoGols = casa.GolsFeitos - casa.GolsSofridos;
                fora.SaldoGols = fora.GolsFeitos - fora.GolsSofridos;
            }

            return tabelaDict.Values
                .OrderByDescending(t => t.Pontos)
                .ThenByDescending(t => t.Vitorias)
                .ThenByDescending(t => t.SaldoGols)
                .ThenByDescending(t => t.GolsFeitos)
                .Select((t, index) =>
                {
                    t.Posicao = index + 1;
                    return t;
                })
                .ToList();
        }

        public async Task<List<ListarJogoDTO>> ObterJogosPontosCorridos(Guid campeonatoId)
        {
            var partidas = await _repositoryGenerics.ObterComIncludes(
                p => p.TimeCasa,
                p => p.TimeVisitante
            );

            return partidas
                .Where(p => p.CampeonatoId == campeonatoId && p.Rodada != null)
                .OrderBy(p => p.Rodada)
                .Select(p => new ListarJogoDTO
                {
                    Id = p.Id,
                    Rodada = p.Rodada ?? 0,
                    NomeTimeCasa = p.TimeCasa?.Nome ?? "A definir",
                    NomeTimeVisitante = p.TimeVisitante?.Nome ?? "A definir",
                    LogoTimeCasa = p.TimeCasa?.LogoUrl,
                    LogoTimeVisitante = p.TimeVisitante?.LogoUrl,
                    GolsTimeCasa = p.GolsTimeCasa,
                    GolsTimeVisitante = p.GolsTimeVisitante,
                    Finalizado = p.Finalizado
                })
                .ToList();
        }

        public async Task<DetalhePartidaDTO> ObterDetalhePartida(Guid partidaId)
        {
            var todas = await _repositoryGenerics.ObterComIncludes(
                p => p.TimeCasa,
                p => p.TimeVisitante
            );

            var partida = todas.FirstOrDefault(p => p.Id == partidaId);
            if (partida == null) throw new Exception("Partida não encontrada.");

            return new DetalhePartidaDTO
            {
                Id = partida.Id,
                CampeonatoId = partida.CampeonatoId,
                Rodada = partida.Rodada,
                Fase = partida.Fase.ToString(),
                NomeTimeCasa = partida.TimeCasa?.Nome ?? "A definir",
                LogoTimeCasa = partida.TimeCasa?.LogoUrl,
                NomeTimeVisitante = partida.TimeVisitante?.Nome ?? "A definir",
                LogoTimeVisitante = partida.TimeVisitante?.LogoUrl,
                GolsTimeCasa = partida.GolsTimeCasa,
                GolsTimeVisitante = partida.GolsTimeVisitante,
                DataHora = partida.DataHora,
                Local = partida.Local,
                Finalizado = partida.Finalizado
            };
        }
    }
}
