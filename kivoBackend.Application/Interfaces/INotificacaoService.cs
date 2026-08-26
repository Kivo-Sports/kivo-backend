using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface INotificacaoService
    {
        Task CriarNotificacaoAsync(Guid usuarioId, string titulo, string mensagem, EnumTipoNotificacao tipo, string? link = null, bool enviarEmail = false);
        Task<List<Notificacao>> ObterNotificacoesUsuarioAsync(Guid usuarioId);
        Task<int> ObterQuantidadeNaoLidasAsync(Guid usuarioId);
        Task MarcarComoLidaAsync(Guid notificacaoId);
        Task MarcarTodasComoLidasAsync(Guid usuarioId);
    }
}
