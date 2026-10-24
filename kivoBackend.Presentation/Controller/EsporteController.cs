using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EsporteController : ControllerBase
    {
        private readonly IServiceGenerics<Esporte> _esporteService;

        public EsporteController(IServiceGenerics<Esporte> esporteService)
        {
            _esporteService = esporteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var esportes = await _esporteService.ObterTodos();
                return Ok(esportes.OrderBy(e => e.Nome).Select(MapearParaDto));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var esporte = await _esporteService.ObterPorId(id);
                if (esporte == null) return NotFound("Esporte não encontrado.");
                return Ok(MapearParaDto(esporte));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Post([FromBody] CriarEsporteDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Nome))
                    return BadRequest(new { message = "O nome do esporte é obrigatório." });
                if (string.IsNullOrWhiteSpace(dto.Icone))
                    return BadRequest(new { message = "Selecione um ícone para o esporte." });

                var novoEsporte = new Esporte
                {
                    Id = Guid.NewGuid(),
                    Nome = dto.Nome.Trim(),
                    Icone = dto.Icone.Trim(),
                    Ativo = true,
                    CriadoEm = DateTime.Now
                };

                var resultado = await _esporteService.Adicionar(novoEsporte);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, MapearParaDto(resultado));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Put(Guid id, [FromBody] EditarEsporteDto dto)
        {
            try
            {
                var esporte = await _esporteService.ObterPorId(id);
                if (esporte == null) return NotFound("Esporte não encontrado.");

                if (string.IsNullOrWhiteSpace(dto.Nome))
                    return BadRequest(new { message = "O nome do esporte é obrigatório." });
                if (string.IsNullOrWhiteSpace(dto.Icone))
                    return BadRequest(new { message = "Selecione um ícone para o esporte." });

                esporte.Nome = dto.Nome.Trim();
                esporte.Icone = dto.Icone.Trim();
                esporte.Ativo = dto.Ativo;

                await _esporteService.Atualizar(esporte);
                return Ok(MapearParaDto(esporte));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            try
            {
                var esporte = await _esporteService.ObterPorId(id);
                if (esporte == null) return NotFound("Esporte não encontrado.");

                esporte.Ativo = !esporte.Ativo;
                await _esporteService.Atualizar(esporte);
                return Ok(MapearParaDto(esporte));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var esporte = await _esporteService.ObterPorId(id);
                if (esporte == null) return NotFound("Esporte não encontrado.");

                await _esporteService.Remover(id);
                return NoContent();
            }
            catch (Exception)
            {
                // FK Restrict: o esporte está vinculado a times/campeonatos.
                return BadRequest(new { message = "Não é possível excluir: existem times ou campeonatos usando este esporte. Desative-o em vez de excluir." });
            }
        }

        private static ListarEsporteDto MapearParaDto(Esporte e)
        {
            return new ListarEsporteDto
            {
                Id = e.Id,
                Nome = e.Nome,
                Icone = e.Icone,
                Ativo = e.Ativo,
                CriadoEm = e.CriadoEm
            };
        }
    }
}
