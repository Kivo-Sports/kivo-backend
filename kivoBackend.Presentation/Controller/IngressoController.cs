using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace kivoBackend.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IngressoController : ControllerBase
    {
        private readonly IIngressoService _ingressoService;

        public IngressoController(IIngressoService ingressoService)
        {
            _ingressoService = ingressoService;
        }

        [HttpPost("comprar")]
        public async Task<IActionResult> Comprar([FromBody] RealizarCompraDTO dto)
        {
            try
            {
                var usuarioIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(usuarioIdStr, out Guid usuarioId))
                    return Unauthorized(new { message = "Usuário não autenticado corretamente." });

                var resultado = await _ingressoService.ComprarIngressosAsync(usuarioId, dto);
                return Ok(resultado);
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

                var ingressos = await _ingressoService.ObterMeusIngressosAsync(usuarioId);
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
                var sucesso = await _ingressoService.ValidarIngressosNaPortariaAsync(codigo);
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
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
