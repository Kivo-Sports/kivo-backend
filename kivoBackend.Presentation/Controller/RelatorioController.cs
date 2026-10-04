using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Presentation.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller;

[ApiController]
[Route("api/relatorios")]
[Authorize]
public class RelatorioController : ControllerBase
{
    private readonly IRelatorioService _relatorioService;
    private readonly ICurrentUserService _currentUser;

    public RelatorioController(IRelatorioService relatorioService, ICurrentUserService currentUser)
    {
        _relatorioService = relatorioService;
        _currentUser = currentUser;
    }

    [HttpGet("organizador-campeonato")]
    [Authorize(Roles = "OrganizadorCampeonato")]
    public async Task<IActionResult> OrganizadorCampeonato([FromQuery] FiltroRelatorioDTO filtro)
    {
        try
        {
            if (!_currentUser.UserId.HasValue) return Unauthorized();
            return Ok(await _relatorioService.ObterOrganizadorCampeonatoAsync(_currentUser.UserId.Value, filtro));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("organizador-time")]
    [Authorize(Roles = "OrganizadorTime")]
    public async Task<IActionResult> OrganizadorTime([FromQuery] FiltroRelatorioDTO filtro)
    {
        try
        {
            if (!_currentUser.UserId.HasValue) return Unauthorized();
            return Ok(await _relatorioService.ObterOrganizadorTimeAsync(_currentUser.UserId.Value, filtro));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("torcedor")]
    [Authorize(Roles = "Torcedor")]
    public async Task<IActionResult> Torcedor([FromQuery] FiltroRelatorioDTO filtro)
    {
        try
        {
            if (!_currentUser.UserId.HasValue) return Unauthorized();
            return Ok(await _relatorioService.ObterTorcedorAsync(_currentUser.UserId.Value, filtro));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("administrativo")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Administrativo([FromQuery] FiltroRelatorioDTO filtro)
    {
        try { return Ok(await _relatorioService.ObterAdministrativoAsync(filtro)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
