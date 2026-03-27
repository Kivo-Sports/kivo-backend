using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/auth")] 
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUsuarioService _usuarioService;
        private readonly TokenService _tokenService;

        public AuthController(UserManager<IdentityUser> userManager, IUsuarioService usuarioService, TokenService tokenService)
        {
            _userManager = userManager;
            _usuarioService = usuarioService;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            IdentityUser? identityUser = null;
            Usuario? usuarioKivo = null;

            if (loginDto.Identificador.Contains("@"))
            {
                identityUser = await _userManager.FindByEmailAsync(loginDto.Identificador);
                if (identityUser != null)
                    usuarioKivo = await _usuarioService.ObterUsuarioPorEmail(identityUser.Email!);
            }
            else
            {
                usuarioKivo = await _usuarioService.ObterUsuarioPorCpf(loginDto.Identificador);
                if (usuarioKivo != null)
                    identityUser = await _userManager.FindByEmailAsync(usuarioKivo.Email!);
            }

            if (identityUser == null || usuarioKivo == null)
                return Unauthorized(new { message = "Usuário não encontrado ou inválido." });

            if (!usuarioKivo.Ativo)
                return Unauthorized(new { message = "Esta conta está desativada." });

            var loginValido = await _userManager.CheckPasswordAsync(identityUser, loginDto.Senha);
            if (!loginValido)
                return Unauthorized(new { message = "Senha incorreta." });

            var token = await _tokenService.GenerateToken(identityUser, usuarioKivo);

            return Ok(new
            {
                token = token,
                usuario = new
                {
                    usuarioKivo.Nome,
                    Cargo = usuarioKivo.EnumCargo.ToString(),
                    usuarioKivo.Id
                }
            });
        }
    }
}