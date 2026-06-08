using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritoController : ControllerBase
    {
        private readonly IFavoritoService _favoritoService;

        public FavoritoController(IFavoritoService favoritoService)
        {
            _favoritoService = favoritoService;
        }

        private Guid? UsuarioLogadoId()
            => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g) ? g : null;

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            var usuarioId = UsuarioLogadoId();
            if (usuarioId == null) return Unauthorized();

            var favoritos = await _favoritoService.ListarPorUsuario(usuarioId.Value);
            return Ok(favoritos);
        }

        [HttpGet("timeline")]
        public async Task<IActionResult> Timeline()
        {
            var usuarioId = UsuarioLogadoId();
            if (usuarioId == null) return Unauthorized();

            var timeline = await _favoritoService.ObterTimeline(usuarioId.Value);
            return Ok(timeline);
        }

        [HttpPost]
        public async Task<IActionResult> Adicionar([FromBody] FavoritoRequestDTO dto)
        {
            try
            {
                var usuarioId = UsuarioLogadoId();
                if (usuarioId == null) return Unauthorized();

                await _favoritoService.Adicionar(usuarioId.Value, dto.Tipo, dto.ItemId);
                return Ok("Favorito adicionado.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete]
        public async Task<IActionResult> Remover([FromBody] FavoritoRequestDTO dto)
        {
            try
            {
                var usuarioId = UsuarioLogadoId();
                if (usuarioId == null) return Unauthorized();

                await _favoritoService.Remover(usuarioId.Value, dto.Tipo, dto.ItemId);
                return Ok("Favorito removido.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
