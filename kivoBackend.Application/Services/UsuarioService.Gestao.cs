using kivoBackend.Application.DTO;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Application.Services;

public partial class UsuarioService
{
    private static void GarantirSemRestricao(Usuario usuario)
    {
        if (usuario.EstadoAdministrativo != EnumEstadoAdministrativo.Normal)
            throw new InvalidOperationException("Conta restrita pela administração. Entre em contato com um administrador.");
    }

    private kivoBackend.Infrastructure.Data.AppDbContext Db => _db ?? throw new InvalidOperationException("Contexto transacional obrigatório.");
    private static GestaoUsuarioException Conflito(string code, string msg, object? impedimentos = null) => new(code, msg, impedimentos);
    private static void ConferirIdentity(IdentityResult result)
    {
        if (!result.Succeeded) throw Conflito("identity_failure", "Não foi possível atualizar a autenticação. Nenhuma alteração foi salva.");
    }

    public async Task<GestaoUsuarioPagina> ListarGestao(GestaoUsuarioFiltro filtro)
    {
        if (filtro.Page < 1 || filtro.PageSize < 1 || filtro.PageSize > 100 || (long)(filtro.Page - 1) * filtro.PageSize > int.MaxValue || (filtro.Cargo.HasValue && !Enum.IsDefined(filtro.Cargo.Value)))
            throw new ArgumentException("Filtros inválidos.");
        var query = Db.Usuarios.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            query = query.Where(u => u.Nome.Contains(busca) || u.Email.Contains(busca));
        }
        if (filtro.Cargo.HasValue) query = query.Where(u => u.EnumCargo == filtro.Cargo);
        query = filtro.Situacao?.ToLowerInvariant() switch
        {
            null or "" => query,
            "ativo" => query.Where(u => u.Ativo && u.EstadoAdministrativo == EnumEstadoAdministrativo.Normal),
            "inativo" => query.Where(u => !u.Ativo && u.EstadoAdministrativo == EnumEstadoAdministrativo.Normal),
            "bloqueado" => query.Where(u => u.EstadoAdministrativo == EnumEstadoAdministrativo.Bloqueado),
            "excluido" => query.Where(u => u.EstadoAdministrativo == EnumEstadoAdministrativo.Excluido),
            _ => throw new ArgumentException("Situação inválida.")
        };
        var total = await query.CountAsync();
        var rows = await query.OrderBy(u => u.Nome).ThenBy(u => u.Id)
            .Skip(checked((filtro.Page - 1) * filtro.PageSize)).Take(filtro.PageSize)
            .Select(u => new { u.Id, u.Nome, u.Email, u.EnumCargo, u.Ativo, u.EstadoAdministrativo,
                Endereco = u.Torcedor != null || u.OrganizadorTime != null || u.OrganizadorCampeonato != null,
                Conta = u.OrganizadorCampeonato != null && u.OrganizadorCampeonato.ContaBanco != null }).ToListAsync();
        return new(rows.Select(u => new GestaoUsuarioItem(u.Id, u.Nome, u.Email, u.EnumCargo.ToString(),
            u.EstadoAdministrativo == EnumEstadoAdministrativo.Normal ? (u.Ativo ? "ativo" : "inativo") : u.EstadoAdministrativo.ToString().ToLowerInvariant(), u.Endereco, u.Conta)), total, filtro.Page, filtro.PageSize);
    }

    private async Task ValidarAutor(Guid autorId)
    {
        if (!await Db.Usuarios.AnyAsync(u => u.Id == autorId && u.Ativo && u.EstadoAdministrativo == EnumEstadoAdministrativo.Normal && u.EnumCargo == EnumCargo.Administrador))
            throw Conflito("admin_invalid", "A conta administrativa não está mais autorizada.");
    }

    private async Task ProtegerAdmin(Usuario usuario, Guid autorId)
    {
        if (usuario.Id == autorId) throw Conflito("self_action", "Não é permitido bloquear, excluir ou rebaixar sua própria conta.");
        if (usuario.EnumCargo == EnumCargo.Administrador && usuario.Ativo && usuario.EstadoAdministrativo == EnumEstadoAdministrativo.Normal &&
            await Db.Usuarios.CountAsync(u => u.EnumCargo == EnumCargo.Administrador && u.Ativo && u.EstadoAdministrativo == EnumEstadoAdministrativo.Normal) <= 1)
            throw Conflito("last_admin", "É necessário manter um administrador ativo.");
    }

    private async Task Revogar(Usuario usuario)
    {
        usuario.VersaoSessao = checked(usuario.VersaoSessao + 1);
        var codes = await Db.VerificationCodes.Where(c => c.UsuarioId == usuario.Id && !c.Usado).ToListAsync();
        foreach (var code in codes) code.Usado = true;
        var identity = await _userManager.FindByEmailAsync(usuario.Email) ?? throw Conflito("identity_missing", "Identidade de autenticação não encontrada.");
        ConferirIdentity(await _userManager.UpdateSecurityStampAsync(identity));
    }

    public Task AlterarEstadoAdministrativo(Guid id, Guid autorId, string acao) => UsuarioGestaoTransaction.Executar(Db, async () =>
    {
        await ValidarAutor(autorId);
        var usuario = await Db.Usuarios.SingleOrDefaultAsync(u => u.Id == id) ?? throw new KeyNotFoundException("Usuário não encontrado.");
        var estado = usuario.EstadoAdministrativo;
        switch (acao)
        {
            case "bloquear" when estado == EnumEstadoAdministrativo.Normal:
                await ProtegerAdmin(usuario, autorId); usuario.EstadoAdministrativo = EnumEstadoAdministrativo.Bloqueado; usuario.Ativo = false; break;
            case "excluir" when estado != EnumEstadoAdministrativo.Excluido:
                await ProtegerAdmin(usuario, autorId); usuario.EstadoAdministrativo = EnumEstadoAdministrativo.Excluido; usuario.Ativo = false; break;
            case "desbloquear" when estado == EnumEstadoAdministrativo.Bloqueado:
            case "restaurar" when estado == EnumEstadoAdministrativo.Excluido:
            case "reativar" when estado == EnumEstadoAdministrativo.Normal && !usuario.Ativo:
                usuario.EstadoAdministrativo = EnumEstadoAdministrativo.Normal; usuario.Ativo = true; break;
            default: throw Conflito("invalid_transition", "A ação não é válida para a situação atual da conta.");
        }
        await Revogar(usuario);
        await Db.SaveChangesAsync();
        return true;
    });

    public Task AlterarCargoAdministrativo(Guid id, Guid autorId, AlterarCargoUsuarioDTO dto) => UsuarioGestaoTransaction.Executar(Db, async () =>
    {
        await ValidarAutor(autorId);
        if (!Enum.IsDefined(dto.Cargo)) throw new ArgumentException("Cargo inválido.");
        var usuario = await Db.Usuarios.Include(u => u.Torcedor).ThenInclude(p => p.Endereco)
            .Include(u => u.OrganizadorTime).ThenInclude(p => p.Endereco)
            .Include(u => u.OrganizadorCampeonato).ThenInclude(p => p.Endereco)
            .Include(u => u.OrganizadorCampeonato).ThenInclude(p => p.ContaBanco)
            .SingleOrDefaultAsync(u => u.Id == id) ?? throw new KeyNotFoundException("Usuário não encontrado.");
        if (usuario.EnumCargo == dto.Cargo) return true;
        if (usuario.EnumCargo == EnumCargo.Administrador) await ProtegerAdmin(usuario, autorId);
        if (dto.Cargo != EnumCargo.Administrador)
        {
            var times = dto.Cargo == EnumCargo.OrganizadorTime ? new List<string>() : await Db.Times.Where(t => t.OrganizadorTime.UsuarioId == id && t.Ativo).Select(t => t.Nome).ToListAsync();
            var campeonatos = dto.Cargo == EnumCargo.OrganizadorCampeonato ? new List<string>() : (await Db.Campeonatos.Where(c => c.OrganizadorCampeonato.UsuarioId == id).ToListAsync()).Where(c => c.EnumStatusCampeonato != EnumStatusCampeonato.Finalizado && c.EnumStatusCampeonato != EnumStatusCampeonato.Cancelado).Select(c => c.Nome).ToList();
            if (times.Count + campeonatos.Count > 0) throw Conflito("management_links", "Encerre ou reatribua as responsabilidades antes de alterar o cargo.", new { times, campeonatos });
        }
        CriarPerfilDestino(usuario, dto);
        var identity = await _userManager.FindByEmailAsync(usuario.Email) ?? throw Conflito("identity_missing", "Identidade de autenticação não encontrada.");
        var kivoRoles = Enum.GetNames<EnumCargo>();
        var antigas = (await _userManager.GetRolesAsync(identity)).Where(r => kivoRoles.Contains(r)).ToArray();
        ConferirIdentity(await _userManager.RemoveFromRolesAsync(identity, antigas));
        ConferirIdentity(await _userManager.AddToRoleAsync(identity, dto.Cargo.ToString()));
        usuario.EnumCargo = dto.Cargo;
        await Revogar(usuario);
        await Db.SaveChangesAsync();
        return true;
    });

    private void CriarPerfilDestino(Usuario u, AlterarCargoUsuarioDTO dto)
    {
        if (dto.Cargo == EnumCargo.Administrador) return;
        var endereco = u.Torcedor?.Endereco ?? u.OrganizadorTime?.Endereco ?? u.OrganizadorCampeonato?.Endereco;
        if (endereco == null)
        {
            var d = dto.DadosPerfil?.Endereco;
            if (d == null || new[] { d.Cep, d.Rua, d.Numero, d.Cidade, d.Estado, d.Pais }.Any(string.IsNullOrWhiteSpace))
                throw Conflito("profile_data_required", "Informe todos os campos obrigatórios do endereço.");
            endereco = new Endereco { Cep = d.Cep.Trim(), Rua = d.Rua.Trim(), Numero = d.Numero.Trim(), Cidade = d.Cidade.Trim(), Estado = d.Estado.Trim(), Pais = d.Pais.Trim(), Complemento = d.Complemento };
        }
        if (dto.Cargo == EnumCargo.Torcedor && u.Torcedor == null)
        {
            u.Torcedor = new Torcedor { Id = Guid.NewGuid(), UsuarioId = u.Id, EnderecoId = endereco.Id, Endereco = endereco };
            Db.Torcedores.Add(u.Torcedor);
        }
        if (dto.Cargo == EnumCargo.OrganizadorTime && u.OrganizadorTime == null)
        {
            u.OrganizadorTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = u.Id, EnderecoId = endereco.Id, Endereco = endereco };
            Db.OrganizadoresTime.Add(u.OrganizadorTime);
        }
        if (dto.Cargo == EnumCargo.OrganizadorCampeonato)
        {
            if (u.OrganizadorCampeonato == null)
            {
                u.OrganizadorCampeonato = new OrganizadorCampeonato { UsuarioId = u.Id, EnderecoId = endereco.Id, Endereco = endereco };
                Db.OrganizadoresCampeonato.Add(u.OrganizadorCampeonato);
            }
            if (u.OrganizadorCampeonato.ContaBanco == null)
            {
                var c = dto.DadosPerfil?.ContaBanco;
                if (c == null || new[] { c.Banco, c.Agencia, c.Conta, c.ChavePix }.Any(string.IsNullOrWhiteSpace)) throw Conflito("profile_data_required", "Informe banco, agência, conta e chave Pix.");
                u.OrganizadorCampeonato.ContaBanco = new ContaBanco { OrganizadorCampeonatoId = u.OrganizadorCampeonato.Id, Banco = c.Banco.Trim(), Agencia = c.Agencia.Trim(), Conta = c.Conta.Trim(), Tipo = c.Tipo, ChavePix = c.ChavePix.Trim() };
                Db.ContasBanco.Add(u.OrganizadorCampeonato.ContaBanco);
            }
        }
    }

    public Task<bool> ResponsavelValido(Guid perfilId, EnumCargo cargo, bool permitirAdmin = false) => Db.Usuarios.AnyAsync(u => u.Ativo && u.EstadoAdministrativo == EnumEstadoAdministrativo.Normal && (u.EnumCargo == cargo || permitirAdmin && u.EnumCargo == EnumCargo.Administrador) &&
        (cargo == EnumCargo.OrganizadorTime ? u.OrganizadorTime != null && u.OrganizadorTime.Id == perfilId : u.OrganizadorCampeonato != null && u.OrganizadorCampeonato.Id == perfilId));
}
