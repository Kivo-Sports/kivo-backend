using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static kivoBackend.Core.Entities.TipoDeficienciaSeed;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TipoDeficienciaController : ControllerBase
    {
        private readonly IServiceGenerics<TipoDeficiencia> _tipoDeficienciaService;

        public TipoDeficienciaController(IServiceGenerics<TipoDeficiencia> tipoDeficienciaService)
        {
            _tipoDeficienciaService = tipoDeficienciaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var opcoes = await _tipoDeficienciaService.ObterTodos();
                return Ok(opcoes
                    .OrderBy(o => o.Id == NenhumaId ? 0 : 1)
                    .ThenBy(o => o.Descricao)
                    .Select(o => new { o.Id, o.Descricao }));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
