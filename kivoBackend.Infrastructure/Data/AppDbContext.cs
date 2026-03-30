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
        public DbSet<CodigoReativacao> CodigosReativacao { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração de unico CPF
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Cpf)
                .IsUnique();

            // Relacionamentos 1:1 - Perfis com Usuário (Cascata para deletar perfil se usuário sumir)
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

            // Relacionamentos com Endereço (Um para Um para cada perfil)
            modelBuilder.Entity<Torcedor>()
                .HasOne(t => t.Endereco)
                .WithOne()
                .HasForeignKey<Torcedor>(t => t.EnderecoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizadorTime>()
                .HasOne(o => o.Endereco)
                .WithOne()
                .HasForeignKey<OrganizadorTime>(o => o.EnderecoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizadorCampeonato>()
                .HasOne(o => o.Endereco)
                .WithOne()
                .HasForeignKey<OrganizadorCampeonato>(o => o.EnderecoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacionamento OrganizadorCampeonato -> ContaBanco
            modelBuilder.Entity<ContaBanco>()
                .HasOne(cb => cb.OrganizadorCampeonato)
                .WithOne(oc => oc.ContaBanco)
                .HasForeignKey<ContaBanco>(cb => cb.OrganizadorCampeonatoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuração da Tabela de Associação (N:N) Campeonato <-> Time
            modelBuilder.Entity<CampeonatoTime>()
                .HasKey(ct => new { ct.CampeonatoId, ct.TimeId });

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

            // Configuração CodigoReativacao -> Usuario
            modelBuilder.Entity<CodigoReativacao>()
                .HasOne(cr => cr.Usuario)
                .WithMany()
                .HasForeignKey(cr => cr.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuração VerificationCode -> Usuario (genérico para todas as verificações)
            modelBuilder.Entity<VerificationCode>()
                .HasOne(vc => vc.Usuario)
                .WithMany()
                .HasForeignKey(vc => vc.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}