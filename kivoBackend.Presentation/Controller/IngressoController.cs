using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Presentation.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngressoController : ControllerBase
    {
        private readonly IIngressoLoteService _ingressoLoteService;
        private readonly IUsuarioService _usuarioService;
        private readonly ICurrentUserService _currentUser;

        public IngressoController(IIngressoLoteService ingressoLoteService, IUsuarioService usuarioService, ICurrentUserService currentUser)
        {
            _ingressoLoteService = ingressoLoteService;
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

        private async Task<Guid?> ObterOrganizadorCampeonatoIdAtual()
        {
            if (!_currentUser.UserId.HasValue)
                return null;

            var usuario = await _usuarioService.ObterUsuarioPorId(_currentUser.UserId.Value);
            return usuario.OrganizadorCampeonato?.Id;
        }
    }
}
