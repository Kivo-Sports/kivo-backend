using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampeonatoController : ControllerBase
    {
        private readonly ICampeonatoService _campeonatoService;

        public CampeonatoController(ICampeonatoService campeonatoService)
        {
            _campeonatoService = campeonatoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _campeonatoService.ObterTodos());

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Campeonato campeonato)
        {
            try
            {
                campeonato.Id = Guid.NewGuid();
                campeonato.CriadoEm = DateTime.Now;
                var resultado = await _campeonatoService.Adicionar(campeonato);
                return Ok(resultado);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var campeonato = await _campeonatoService.ObterPorId(id);

                if (campeonato == null)
                {
                    return NotFound("Campeonato não encontrado.");
                }

                return Ok(campeonato);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _campeonatoService.Remover(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
