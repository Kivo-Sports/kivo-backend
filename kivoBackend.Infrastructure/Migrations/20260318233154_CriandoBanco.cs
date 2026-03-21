using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CriandoBanco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Enderecos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enderecos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Senha = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnumCargo = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Administradores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administradores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Administradores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizadoresCampeonato",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnderecoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizadoresCampeonato", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizadoresCampeonato_Enderecos_EnderecoId",
                        column: x => x.EnderecoId,
                        principalTable: "Enderecos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizadoresCampeonato_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizadoresTime",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnderecoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizadoresTime", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizadoresTime_Enderecos_EnderecoId",
                        column: x => x.EnderecoId,
                        principalTable: "Enderecos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizadoresTime_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecuperacoesSenha",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Usado = table.Column<bool>(type: "bit", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecuperacoesSenha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecuperacoesSenha_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Torcedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnderecoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Torcedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Torcedores_Enderecos_EnderecoId",
                        column: x => x.EnderecoId,
                        principalTable: "Enderecos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Torcedores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Campeonatos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizadorCampeonatoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PontosVitoria = table.Column<int>(type: "int", nullable: false),
                    PontosDerrota = table.Column<int>(type: "int", nullable: false),
                    PontosEmpate = table.Column<int>(type: "int", nullable: false),
                    EnumStatusCampeonato = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campeonatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campeonatos_OrganizadoresCampeonato_OrganizadorCampeonatoId",
                        column: x => x.OrganizadorCampeonatoId,
                        principalTable: "OrganizadoresCampeonato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContasBanco",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizadorCampeonatoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Agencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Conta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChavePix = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasBanco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasBanco_OrganizadoresCampeonato_OrganizadorCampeonatoId",
                        column: x => x.OrganizadorCampeonatoId,
                        principalTable: "OrganizadoresCampeonato",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Times",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizadorTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Times", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Times_OrganizadoresTime_OrganizadorTimeId",
                        column: x => x.OrganizadorTimeId,
                        principalTable: "OrganizadoresTime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampeonatoTimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampeonatoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnumStatusParticipacao = table.Column<int>(type: "int", nullable: false),
                    ConvidadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondidoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondidoPorOrganizadorTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampeonatoTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampeonatoTimes_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "Campeonatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampeonatoTimes_Times_TimeId",
                        column: x => x.TimeId,
                        principalTable: "Times",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Administradores_UsuarioId",
                table: "Administradores",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campeonatos_OrganizadorCampeonatoId",
                table: "Campeonatos",
                column: "OrganizadorCampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampeonatoTimes_CampeonatoId",
                table: "CampeonatoTimes",
                column: "CampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampeonatoTimes_TimeId",
                table: "CampeonatoTimes",
                column: "TimeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasBanco_OrganizadorCampeonatoId",
                table: "ContasBanco",
                column: "OrganizadorCampeonatoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresCampeonato_EnderecoId",
                table: "OrganizadoresCampeonato",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresCampeonato_UsuarioId",
                table: "OrganizadoresCampeonato",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresTime_EnderecoId",
                table: "OrganizadoresTime",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresTime_UsuarioId",
                table: "OrganizadoresTime",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecuperacoesSenha_UsuarioId",
                table: "RecuperacoesSenha",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Times_OrganizadorTimeId",
                table: "Times",
                column: "OrganizadorTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_Torcedores_EnderecoId",
                table: "Torcedores",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_Torcedores_UsuarioId",
                table: "Torcedores",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cpf",
                table: "Usuarios",
                column: "Cpf",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Administradores");

            migrationBuilder.DropTable(
                name: "CampeonatoTimes");

            migrationBuilder.DropTable(
                name: "ContasBanco");

            migrationBuilder.DropTable(
                name: "RecuperacoesSenha");

            migrationBuilder.DropTable(
                name: "Torcedores");

            migrationBuilder.DropTable(
                name: "Campeonatos");

            migrationBuilder.DropTable(
                name: "Times");

            migrationBuilder.DropTable(
                name: "OrganizadoresCampeonato");

            migrationBuilder.DropTable(
                name: "OrganizadoresTime");

            migrationBuilder.DropTable(
                name: "Enderecos");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
