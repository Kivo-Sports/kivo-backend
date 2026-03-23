using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IServiceGenerics<Usuario> _serviceGenerics;
        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var retorno = await _usuarioService.ObterTodosUsuarios();
                return Ok(retorno);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var usuario = await _usuarioService.ObterUsuarioPorId(id);
                return Ok(usuario);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("cpf/{cpf}")]
        public async Task<IActionResult> ObterPorCpf(string cpf)
        {
            var usuario = await _usuarioService.ObterUsuarioPorCpf(cpf);
            if (usuario == null) return NotFound("Usuário não encontrado com este CPF.");

            return Ok(usuario);
        }

        [HttpPost("torcedor")]
        public async Task<IActionResult> CriarTorcedor([FromBody] CriarTorcedorDto dto)
        {
            try
            {
                var usuario = MapearBase(dto, EnumCargo.Torcedor);

                usuario.Torcedor = new Torcedor
                {
                    Endereco = new Endereco
                    {
                        Cep = dto.Endereco.Cep,
                        Rua = dto.Endereco.Rua,
                        Numero = dto.Endereco.Numero,
                        Cidade = dto.Endereco.Cidade,
                        Estado = dto.Endereco.Estado,
                        Complemento = "",
                        Pais = "Brasil"
                    }
                };

                var resultado = await _usuarioService.CriarUsuario(usuario, dto.Senha);

                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("organizador-time")]
        public async Task<IActionResult> PostOrganizadorTime([FromBody] CriarOrganizadorTimeDto dto)
        {
            try
            {
                var usuario = MapearBase(dto, EnumCargo.OrganizadorTime);

                usuario.OrganizadorTime = new OrganizadorTime
                {
                    Endereco = new Endereco
                    {
                        Cep = dto.Endereco.Cep,
                        Rua = dto.Endereco.Rua,
                        Numero = dto.Endereco.Numero,
                        Cidade = dto.Endereco.Cidade,
                        Estado = dto.Endereco.Estado,
                        Complemento = "",
                        Pais = "Brasil"
                    }
                };

                var novoUsuario = await _usuarioService.CriarUsuario(usuario, dto.Senha);
                return CreatedAtAction(nameof(GetById), new { id = novoUsuario.Id }, novoUsuario);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("organizador-campeonato")]
        public async Task<IActionResult> PostOrganizadorCampeonato([FromBody] CriarOrganizadorCampeonatoDto dto)
        {
            try
            {
                var usuario = MapearBase(dto, EnumCargo.OrganizadorCampeonato);

                usuario.OrganizadorCampeonato = new OrganizadorCampeonato
                {
                    Endereco = new Endereco
                    {
                        Cep = dto.Endereco.Cep,
                        Rua = dto.Endereco.Rua,
                        Numero = dto.Endereco.Numero,
                        Cidade = dto.Endereco.Cidade,
                        Estado = dto.Endereco.Estado,
                        Complemento = "",
                        Pais = "Brasil"
                    },
                    ContaBanco = new ContaBanco
                    {
                        Banco = dto.ContaBanco.Banco,
                        Agencia = dto.ContaBanco.Agencia,
                        Conta = dto.ContaBanco.Conta,
                        ChavePix = dto.ContaBanco.ChavePix
                    }
                };

                var novoUsuario = await _usuarioService.CriarUsuario(usuario, dto.Senha);
                return CreatedAtAction(nameof(GetById), new { id = novoUsuario.Id }, novoUsuario);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("admin")]
        public async Task<IActionResult> PostAdmin([FromBody] UsuarioDTO dto)
        {
            try
            {
                var usuario = MapearBase(dto, EnumCargo.Administrador);
                var novoUsuario = await _usuarioService.CriarUsuario(usuario, dto.Senha);
                return CreatedAtAction(nameof(GetById), new { id = novoUsuario.Id }, novoUsuario);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
        private Usuario MapearBase(UsuarioDTO dto, EnumCargo cargo)
        {
            return new Usuario
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Cpf = dto.Cpf,
                Telefone = dto.Telefone,
                DataNascimento = dto.DataNascimento,
                EnumCargo = cargo
            };
        }

        [HttpPut("torcedor/{id}")]
        public async Task<IActionResult> EditarTorcedor(Guid id, [FromBody] EditarUsuarioDTO dto)
        {
            try
            {
                var usuario = MapearParaUpdate(dto);
                usuario.Torcedor = new Torcedor { Endereco = MapearEndereco(dto.Endereco) };

                var resultado = await _usuarioService.EditarDadosUsuario(id, usuario);
                return Ok(resultado);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("organizador-campeonato/{id}")]
        public async Task<IActionResult> EditarOrganizadorCampeonato(Guid id, [FromBody] EditarOrganizadorCampeonatoDTO dto)
        {
            try
            {
                var usuario = MapearParaUpdate(dto);
                usuario.OrganizadorCampeonato = new OrganizadorCampeonato
                {
                    Endereco = MapearEndereco(dto.Endereco),
                    ContaBanco = new ContaBanco
                    {
                        Banco = dto.ContaBanco.Banco,
                        Agencia = dto.ContaBanco.Agencia,
                        Conta = dto.ContaBanco.Conta,
                        ChavePix = dto.ContaBanco.ChavePix
                    }
                };

                var resultado = await _usuarioService.EditarDadosUsuario(id, usuario);
                return Ok(resultado);
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
        private Usuario MapearParaUpdate(EditarUsuarioDTO dto) => new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Telefone = dto.Telefone,
            DataNascimento = dto.DataNascimento
        };

        private Endereco MapearEndereco(EnderecoDto d) => new Endereco
        {
            Cep = d.Cep,
            Rua = d.Rua,
            Numero = d.Numero,
            Cidade = d.Cidade,
            Estado = d.Estado
        };

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _usuarioService.RemoverUsuario(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPatch("{id}/reativar")]
        public async Task<IActionResult> Reativar(Guid id)
        {
            try
            {
                await _usuarioService.AtivarConta(id);
                return Ok("Conta reativada com sucesso.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
