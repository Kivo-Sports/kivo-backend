using System.Security.Claims;
using kivoBackend.Core.Enums;
using kivoBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Application.Services;

public static class UsuarioSessionValidation
{
    public const string VersionClaim = "VersaoSessao";
    public static async Task<bool> Valida(AppDbContext db, ClaimsPrincipal? principal)
    {
        if (!Guid.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ||
            !int.TryParse(principal.FindFirst(VersionClaim)?.Value, out var version)) return false;
        var usuario = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
        if (usuario == null || !usuario.Ativo || usuario.EstadoAdministrativo != EnumEstadoAdministrativo.Normal || usuario.VersaoSessao != version) return false;
        var cargo = usuario.EnumCargo.ToString();
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (principal.FindFirst("Cargo")?.Value != cargo || roles.Length != 1 || roles[0] != cargo) return false;
        var identityId = await db.Users.Where(u => u.NormalizedEmail == usuario.Email.ToUpper()).Select(u => u.Id).SingleOrDefaultAsync();
        var storedRoles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId == identityId select r.Name).ToListAsync();
        return storedRoles.Count(r => Enum.GetNames<EnumCargo>().Contains(r)) == 1 && storedRoles.Contains(cargo);
    }
}
