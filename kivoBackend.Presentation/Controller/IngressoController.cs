using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Presentation.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IngressoController : ControllerBase
    {
        private readonly IIngressoLoteService _ingressoLoteService;
        private readonly IIngressoService? _ingressoService;
        private readonly IUsuarioService _usuarioService;
        private readonly ICurrentUserService _currentUser;

        public IngressoController(
            IIngressoLoteService ingressoLoteService,
            IUsuarioService usuarioService,
            ICurrentUserService currentUser,
            IIngressoService? ingressoService = null)
        {
            _ingressoLoteService = ingressoLoteService;
            _ingressoService = ingressoService;
            _usuarioService = usuarioService;
            _currentUser = currentUser;
        }

        [HttpPost("lote")]
        [Authorize(Roles = "Administrador,OrganizadorCampeonato")]
        public async Task<IActionResult> CriarLote([FromBody] CriarIngressoLoteDTO dto)
        {
            try
            {
                var organizadorCampeonatoId = await ObterOrganizadorCampeonatoIdAtual();
                if (!_currentUser.IsAdmin && organizadorCampeonatoId == null)
                    return Forbid();

                var loteCriado = await _ingressoLoteService.CriarLote(dto, organizadorCampeonatoId, _currentUser.IsAdmin);
                return Ok(loteCriado);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("comprar")]
        public async Task<IActionResult> Comprar([FromBody] RealizarCompraDTO dto)
        {
            try
            {
                var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(usuarioIdStr, out Guid usuarioId))
                    return Unauthorized(new { message = "Usuário não autenticado corretamente." });

                var resultado = await IngressoService().ComprarIngressosAsync(usuarioId, dto);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("partida/{partidaId}/lotes")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterLotesPorPartida(Guid partidaId)
        {
            try
            {
                var lotes = await _ingressoLoteService.ObterLotesPorPartida(partidaId);
                var retorno = lotes.Select(l => new ListarIngressoLoteDTO
                {
                    Id = l.Id,
                    PartidaId = l.PartidaId,
                    NomeLote = l.NomeLote,
                    Preco = l.Preco,
                    QuantidadeTotal = l.QuantidadeTotal,
                    QuantidadeDisponivel = l.QuantidadeDisponivel,
                    Ativo = l.Ativo
                });
                return Ok(retorno);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("meus-ingressos")]
        public async Task<IActionResult> ObterMeusIngressos()
        {
            try
            {
                var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(usuarioIdStr, out Guid usuarioId))
                    return Unauthorized(new { message = "Usuário não autenticado." });

                var ingressos = await IngressoService().ObterMeusIngressosAsync(usuarioId);
                return Ok(ingressos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("validar-portaria/{codigo}")]
        [Authorize(Roles = "Administrador,OrganizadorCampeonato")]
        public async Task<IActionResult> ValidarPortaria(string codigo)
        {
            try
            {
                await IngressoService().ValidarIngressosNaPortariaAsync(codigo);
                return Ok(new { message = "Ingresso validado com sucesso! Entrada liberada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("pagar/{ingressoId}")]
        public async Task<IActionResult> ConfirmarPagamento(Guid ingressoId)
        {
            try
            {
                var sucesso = await _ingressoService.ConfirmarPagamentoAsync(ingressoId);
                return Ok(new { message = "Pagamento confirmado com sucesso! Seus ingressos foram liberados." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{ingressoId}/titular")]
        public async Task<IActionResult> AtribuirTitular(Guid ingressoId, [FromBody] AtribuirTitularIngressoDTO dto)
        {
            try
            {
                var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(usuarioIdStr, out var usuarioId))
                    return Unauthorized(new { message = "Usuário não autenticado corretamente." });

                await _ingressoService.AtribuirTitularAsync(usuarioId, ingressoId, dto);
                return Ok(new { message = "Titular atribuído com sucesso! O QR Code de entrada foi liberado." });
                await IngressoService().ConfirmarPagamentoAsync(ingressoId);
                return Ok(new { message = "Pagamento confirmado com sucesso! Seu QR Code de entrada foi liberado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<Guid?> ObterOrganizadorCampeonatoIdAtual()
        {
            if (!_currentUser.UserId.HasValue)
                return null;

            var usuario = await _usuarioService.ObterUsuarioPorId(_currentUser.UserId.Value);
            return usuario.OrganizadorCampeonato?.Id;
        }

        private IIngressoService IngressoService()
        {
            return _ingressoService
                ?? throw new InvalidOperationException("Serviço de ingressos não configurado.");
        }

    }
}
