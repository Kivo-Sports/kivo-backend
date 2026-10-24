using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartidaController : ControllerBase
    {
        private readonly IPartidaService _partidaService;
        private readonly ICampeonatoService _campeonatoService;
        private readonly IUsuarioService _usuarioService;
        private readonly ICurrentUserService _currentUser;

        public PartidaController(IPartidaService partidaService, ICampeonatoService campeonatoService, IUsuarioService usuarioService, ICurrentUserService currentUser)
        {
            _partidaService = partidaService;
            _campeonatoService = campeonatoService;
            _usuarioService = usuarioService;
            _currentUser = currentUser;
        }

        [HttpPost("gerar-tabela/{campeonatoId}")]
        [Authorize(Roles = "Administrador,OrganizadorCampeonato")]
        public async Task<IActionResult> Gerar(Guid campeonatoId)
        {
            try
            {
                if (!await PodeAlterarCampeonato(campeonatoId)) return Forbid();

                await _partidaService.GerarTabela(campeonatoId);
                return Ok("Jogos gerados com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/atualizar-placar")]
        [Authorize(Roles = "Administrador,OrganizadorCampeonato")]
        public async Task<IActionResult> AtualizarPlacar(Guid id, [FromBody] AtualizarPlacarDTO dto)
        {
            try
            {
                var partida = await _partidaService.ObterPorId(id);
                if (partida == null) return NotFound();
                if (!await PodeAlterarCampeonato(partida.CampeonatoId)) return Forbid();

                if (partida.Finalizado) return BadRequest("Esta partida já foi encerrada e não pode ser editada.");
                if (partida.DataHora > DateTime.Now) return BadRequest("Não é possível atualizar o placar de uma partida que ainda não ocorreu.");

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
        [Authorize]
        public async Task<IActionResult> GetTabela(Guid campeonatoId)
        {
            var classificacao = await _partidaService.ObterClassificacaoTabela(campeonatoId);
            return Ok(classificacao);
        }

        [HttpGet("chaveamento/{campeonatoId}")]
        [Authorize]
        public async Task<IActionResult> GetChaveamento(Guid campeonatoId)
        {
            var chaves = await _partidaService.ObterChaveamentoMataMata(campeonatoId);
            return Ok(chaves);
        }

        [HttpGet("jogos/{campeonatoId}")]
        [Authorize]
        public async Task<IActionResult> GetJogos(Guid campeonatoId)
        {
            var jogos = await _partidaService.ObterJogosPontosCorridos(campeonatoId);
            return Ok(jogos);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var partida = await _partidaService.ObterDetalhePartida(id);
                return Ok(partida);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/agendar")]
        [Authorize(Roles = "Administrador,OrganizadorCampeonato")]
        public async Task<IActionResult> Agendar(Guid id, [FromBody] AgendarPartidaDTO dto)
        {
            try
            {
                var partida = await _partidaService.ObterPorId(id);
                if (partida == null) return NotFound();
                if (!await PodeAlterarCampeonato(partida.CampeonatoId)) return Forbid();

                partida.DataHora = dto.DataHora;
                partida.Local = dto.Local ?? string.Empty;

                await _partidaService.Atualizar(partida);
                return Ok("Agendamento atualizado");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // ---- Endpoints administrativos ----

        [HttpPost("admin")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CriarManual([FromBody] CriarPartidaManualDTO dto)
        {
            try
            {
                var partida = await _partidaService.CriarPartidaManual(dto);
                return Ok(new { partida.Id });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}/admin")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditarAdmin(Guid id, [FromBody] EditarPartidaAdminDTO dto)
        {
            try
            {
                await _partidaService.EditarPartidaAdmin(id, dto);
                return Ok("Jogo atualizado com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}/admin")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeletarAdmin(Guid id)
        {
            try
            {
                await _partidaService.Remover(id);
                return Ok("Jogo excluído com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPatch("{id}/admin-placar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AtualizarPlacarAdmin(Guid id, [FromBody] AtualizarPlacarDTO dto)
        {
            try
            {
                await _partidaService.AtualizarPlacarAdmin(id, dto);
                return Ok("Placar atualizado com sucesso.");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        private async Task<bool> PodeAlterarCampeonato(Guid campeonatoId)
        {
            if (_currentUser.IsAdmin)
                return true;

            if (!_currentUser.UserId.HasValue)
                return false;

            var usuario = await _usuarioService.ObterUsuarioPorId(_currentUser.UserId.Value);
            if (usuario.OrganizadorCampeonato == null)
                return false;

            var campeonato = await _campeonatoService.ObterCampeonatoPorId(campeonatoId);
            return campeonato.OrganizadorCampeonatoId == usuario.OrganizadorCampeonato.Id;
        }
    }
}
