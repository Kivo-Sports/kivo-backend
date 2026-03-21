using kivoBackend.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Torcedor> Torcedores { get; set; }
        public DbSet<OrganizadorTime> OrganizadoresTime { get; set; }
        public DbSet<OrganizadorCampeonato> OrganizadoresCampeonato { get; set; }
        public DbSet<Administrador> Administradores { get; set; }
        public DbSet<Endereco> Enderecos { get; set; }
        public DbSet<ContaBanco> ContasBanco { get; set; }
        public DbSet<Time> Times { get; set; }
        public DbSet<Campeonato> Campeonatos { get; set; }
        public DbSet<CampeonatoTime> CampeonatoTimes { get; set; }
        public DbSet<RecuperacaoSenha> RecuperacoesSenha { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Cpf)
                .IsUnique();

            modelBuilder.Entity<Torcedor>()
                .HasOne(t => t.Usuario)
                .WithOne(u => u.Torcedor)
                .HasForeignKey<Torcedor>(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizadorTime>()
                .HasOne(o => o.Usuario)
                .WithOne(u => u.OrganizadorTime)
                .HasForeignKey<OrganizadorTime>(o => o.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizadorCampeonato>()
                .HasOne(o => o.Usuario)
                .WithOne(u => u.OrganizadorCampeonato)
                .HasForeignKey<OrganizadorCampeonato>(o => o.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Administrador>()
                .HasOne(a => a.Usuario)
                .WithOne(u => u.Administrador)
                .HasForeignKey<Administrador>(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContaBanco>()
                .HasOne(cb => cb.OrganizadorCampeonato)
                .WithOne(oc => oc.ContaBanco)
                .HasForeignKey<ContaBanco>(cb => cb.OrganizadorCampeonatoId);

            modelBuilder.Entity<CampeonatoTime>()
                .HasOne(ct => ct.Campeonato)
                .WithMany(c => c.CampeonatoTimes)
                .HasForeignKey(ct => ct.CampeonatoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CampeonatoTime>()
                .HasOne(ct => ct.Time)
                .WithMany()
                .HasForeignKey(ct => ct.TimeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}