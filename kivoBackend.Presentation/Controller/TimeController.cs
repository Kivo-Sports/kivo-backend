using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeController : ControllerBase
    {
        private readonly ITimeService _timeService;

        public TimeController(ITimeService timeService)
        {
            _timeService = timeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var times = await _timeService.ObterTodos();

                var retorno = times.Select(t => MapearParaDto(t));

                return Ok(retorno);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var time = await _timeService.ObterPorId(id);

                if (time == null)
                {
                    return NotFound("Time não encontrado.");
                }

                return Ok(MapearParaDto(time));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        private ListarTimeDto MapearParaDto(Time t)
        {
            return new ListarTimeDto
            {
                Id = t.Id,
                Nome = t.Nome,
                Cidade = t.Cidade,
                Estado = t.Estado,
                LogoUrl = t.LogoUrl,
                Ativo = t.Ativo,
                CriadoEm = t.CriadoEm,
                OrganizadorTimeId = t.OrganizadorTimeId
            };
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CriarTimeDto dto)
        {
            try
            {
                var novoTime = new Time
                {
                    Id = Guid.NewGuid(),
                    OrganizadorTimeId = dto.OrganizadorTimeId,
                    Nome = dto.Nome,
                    Cidade = dto.Cidade,
                    Estado = dto.Estado,
                    LogoUrl = dto.LogoUrl,
                    Ativo = true,
                    CriadoEm = DateTime.Now
                };

                var resultado = await _timeService.Adicionar(novoTime);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _timeService.Remover(id);
            return NoContent();
        }
    }
}
