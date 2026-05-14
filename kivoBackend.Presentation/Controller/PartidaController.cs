using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartidaController : ControllerBase
    {
        private readonly IPartidaService _partidaService;

        public PartidaController(IPartidaService partidaService)
        {
            _partidaService = partidaService;
        }
        [HttpPost("gerar-tabela/{campeonatoId}")]
        public async Task<IActionResult> Gerar(Guid campeonatoId)
        {
            try
            {
                await _partidaService.GerarTabela(campeonatoId);
                return Ok("Jogos gerados com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/atualizar-placar")]
        public async Task<IActionResult> AtualizarPlacar(Guid id, [FromBody] AtualizarPlacarDTO dto)
        {
            try
            {
                var partida = await _partidaService.ObterPorId(id);
                if (partida == null) return NotFound();
                if (partida.Finalizado) return BadRequest("Esta partida já foi encerrada e não pode ser editada.");

                partida.GolsTimeCasa = dto.GolsTimeCasa;
                partida.GolsTimeVisitante = dto.GolsTimeVisitante;
                partida.Finalizado = true;

                await _partidaService.Atualizar(partida);

                if (partida.Fase != EnumFaseMataMata.Nenhuma)
                {
                    await _partidaService.AtualizarPlacarMataMata(partida);
                }

                else
                {
                    await _partidaService.VerificarFimFasePontosCorridos(partida.CampeonatoId);
                }

                return Ok("Placar atualizado");
            }
            catch (Exception ex) 
            { 
                return BadRequest(ex.Message); 
            }

        }

        [HttpGet("tabela/{campeonatoId}")]
        public async Task<IActionResult> GetTabela(Guid campeonatoId)
        {
            var classificacao = await _partidaService.ObterClassificacaoTabela(campeonatoId);
            return Ok(classificacao);
        }

        [HttpGet("chaveamento/{campeonatoId}")]
        public async Task<IActionResult> GetChaveamento(Guid campeonatoId)
        {
            var chaves = await _partidaService.ObterChaveamentoMataMata(campeonatoId);
            return Ok(chaves);
        }
    }
}
