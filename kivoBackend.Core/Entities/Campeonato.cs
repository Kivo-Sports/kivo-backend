using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Campeonato
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizadorCampeonatoId { get; set; }

        public string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public int PontosVitoria { get; set; }
        public int PontosDerrota { get; set; }
        public int PontosEmpate { get; set; }

        private EnumStatusCampeonato _statusBase;
        public EnumStatusCampeonato EnumStatusCampeonato
        {
            get
            {
                var agora = DateTime.Now;

                if (_statusBase == EnumStatusCampeonato.Rascunho || _statusBase == EnumStatusCampeonato.Cancelado)
                {
                    return _statusBase;
                }

                if (agora > DataFim)
                    return EnumStatusCampeonato.Finalizado;

                if (agora >= DataInicio)
                    return EnumStatusCampeonato.EmAndamento;

                return EnumStatusCampeonato.InscricoesAbertas;
            }
            set
            {
                _statusBase = value;
            }
        }
        public DateTime CriadoEm { get; set; }

        public OrganizadorCampeonato OrganizadorCampeonato { get; set; }

        public ICollection<CampeonatoTime> CampeonatoTimes { get; set; }

        
    }
}
