using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ingressosPartida1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngressoLotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartidaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NomeLote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Preco = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantidadeTotal = table.Column<int>(type: "int", nullable: false),
                    QuantidadeDisponivel = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngressoLotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngressoLotes_Partida_PartidaId",
                        column: x => x.PartidaId,
                        principalTable: "Partida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngressoLotes_PartidaId",
                table: "IngressoLotes",
                column: "PartidaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngressoLotes");
        }
    }
}
