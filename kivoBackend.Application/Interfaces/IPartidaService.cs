using kivoBackend.Application.DTO;
using kivoBackend.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IPartidaService : IServiceGenerics<Partida>
    {
       Task GerarTabela(Guid campeonatoId);
       Task AtualizarPlacarMataMata(Partida partidaFinalizada);
        Task VerificarFimFasePontosCorridos(Guid campeonatoId);
        Task GerarPontosCorridos(Guid campeonatoId, List<Guid> times);
        Task<IEnumerable<ChaveamentoDTO>> ObterChaveamentoMataMata(Guid campeonatoId);
        Task<List<TabelaClassificacaoDTO>> ObterClassificacaoTabela(Guid campeonatoId);
        Task<List<CriterioDesempateDTO>> ObterClassificacaoProximaFase(Guid campeonatoId);
    }
}
