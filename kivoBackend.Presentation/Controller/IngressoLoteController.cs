using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngressoLoteController : ControllerBase
    {
        private readonly IIngressoLoteService _ingressoLoteService;

        public IngressoLoteController(IIngressoLoteService ingressoLoteService)
        {
            _ingressoLoteService = ingressoLoteService;
        }

        [HttpPost("lote")]
        [Authorize]
        public async Task<IActionResult> CriarLote([FromBody] CriarIngressoLoteDTO dto)
        {
            try
            {
                var loteCriado = await _ingressoLoteService.CriarLote(dto);
                return Ok(loteCriado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("partida/{partidaId}/lotes")]
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
    }
}
