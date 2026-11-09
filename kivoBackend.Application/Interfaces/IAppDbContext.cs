using kivoBackend.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Torcedor> Torcedores { get; }
    DbSet<OrganizadorTime> OrganizadoresTime { get; }
    DbSet<OrganizadorCampeonato> OrganizadoresCampeonato { get; }
    DbSet<ContaBanco> ContasBanco { get; }
    DbSet<Time> Times { get; }
    DbSet<Campeonato> Campeonatos { get; }
    DbSet<CampeonatoTime> CampeonatoTimes { get; }
    DbSet<Partida> Partidas { get; }
    DbSet<VerificationCode> VerificationCodes { get; }
    DbSet<Favorito> Favoritos { get; }
    DbSet<IngressoLote> IngressoLotes { get; }
    DbSet<Ingresso> Ingressos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
