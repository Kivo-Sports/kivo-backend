using kivoBackend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificacaoController : ControllerBase
    {
        private readonly INotificacaoService _notificacaoService;

        public NotificacaoController(INotificacaoService notificacaoService)
        {
            _notificacaoService = notificacaoService;
        }

        private Guid ObterUsuarioIdToken() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> ListarMinhasNotificacoes()
        {
            var lista = await _notificacaoService.ObterNotificacoesUsuarioAsync(ObterUsuarioIdToken());
            return Ok(lista);
        }

        [HttpGet("contador-nao-lidas")]
        public async Task<IActionResult> ObterNaoLidas()
        {
            var count = await _notificacaoService.ObterQuantidadeNaoLidasAsync(ObterUsuarioIdToken());
            return Ok(new { naoLidas = count });
        }

        [HttpPut("{id}/ler")]
        public async Task<IActionResult> MarcarLida(Guid id)
        {
            await _notificacaoService.MarcarComoLidaAsync(id);
            return NoContent();
        }

        [HttpPut("ler-todas")]
        public async Task<IActionResult> MarcarTodasLidas()
        {
            await _notificacaoService.MarcarTodasComoLidasAsync(ObterUsuarioIdToken());
            return NoContent();
        }
    }
}
