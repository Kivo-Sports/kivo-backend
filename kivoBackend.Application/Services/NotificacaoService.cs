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
    public class NotificacaoService : INotificacaoService
    {
        private readonly IRepositoryGenerics<Notificacao> _notificacaoRepository;
        private readonly IRepositoryGenerics<Usuario> _usuarioRepository;
        private readonly IEmailService _emailService;

        public NotificacaoService(
            IRepositoryGenerics<Notificacao> notificacaoRepo,
            IRepositoryGenerics<Usuario> usuarioRepo,
            IEmailService emailService)
        {
            _notificacaoRepository = notificacaoRepo;
            _usuarioRepository = usuarioRepo;
            _emailService = emailService;
        }

        public async Task CriarNotificacaoAsync(
    Guid usuarioId,
    string titulo,
    string mensagem,
    EnumTipoNotificacao tipo,
    string? link = null,
    bool enviarEmail = false)
        {
            var notificacao = new Notificacao
            {
                UsuarioId = usuarioId,
                Titulo = titulo,
                Mensagem = mensagem,
                Tipo = tipo,
                LinkRedirecionamento = link,
                CriadaEm = DateTime.UtcNow
            };

            await _notificacaoRepository.Adicionar(notificacao);

            if (enviarEmail)
            {
                var usuario = await _usuarioRepository.ObterPorId(usuarioId);
                if (usuario != null && !string.IsNullOrWhiteSpace(usuario.Email))
                {
                    try
                    {
                        Console.WriteLine($"[SMTP] Enviando e-mail para: {usuario.Email}...");
                        await _emailService.EnviarEmailNotificacaoAsync(
                            usuario.Email,
                            usuario.Nome,
                            titulo,
                            mensagem,
                            link
                        );
                        Console.WriteLine("[SMTP] E-mail enviado com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[ERRO SMTP]: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"[ERRO SMTP DETALHE]: {ex.InnerException.Message}");
                        }
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine($"[SMTP ALERTA]: Usuário {usuarioId} não possui e-mail cadastrado.");
                }
            }
        }

        public async Task<List<Notificacao>> ObterNotificacoesUsuarioAsync(Guid usuarioId)
        {
            var notificacoes = await _notificacaoRepository.Buscar(n => n.UsuarioId == usuarioId);
            return notificacoes.OrderByDescending(n => n.CriadaEm).Take(30).ToList();
        }

        public async Task<int> ObterQuantidadeNaoLidasAsync(Guid usuarioId)
        {
            var naoLidas = await _notificacaoRepository.Buscar(n => n.UsuarioId == usuarioId && !n.Lida);
            return naoLidas.Count();
        }

        public async Task MarcarComoLidaAsync(Guid notificacaoId)
        {
            var notif = await _notificacaoRepository.ObterPorId(notificacaoId);
            if (notif != null)
            {
                notif.Lida = true;
                await _notificacaoRepository.Atualizar(notif);
            }
        }

        public async Task MarcarTodasComoLidasAsync(Guid usuarioId)
        {
            var naoLidas = await _notificacaoRepository.Buscar(n => n.UsuarioId == usuarioId && !n.Lida);
            foreach (var notif in naoLidas)
            {
                notif.Lida = true;
                await _notificacaoRepository.Atualizar(notif);
            }
        }
    }
}
