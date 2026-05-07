using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Enums
{
    public enum EnumStatusCampeonato
    {
        // ETAPA DE PLANEJAMENTO
        Rascunho = 0,      // O organizador está criando, mas ninguém vê ainda
        InscricoesAbertas = 1, // Atletas/Times já podem se inscrever
        InscricoesEncerradas = 2, // Prazo de inscrição acabou, preparando o sorteio/tabela

        // ETAPA DE EXECUÇÃO
        Pausado = 3,       // Houve algum problema (chuva, falta de luz) e o torneio parou
        EmAndamento = 4,   // Os jogos estão acontecendo

        // ETAPA DE FINALIZAÇÃO
        Finalizado = 5,    // O campeão foi definido e o torneio acabou
        Cancelado = 6   // O campeonato foi cancelado por algum motivo
    }
}
