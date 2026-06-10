using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEsporte : Migration
    {
        /// <inheritdoc />
        // Esporte padrão (semente) atribuído aos times/campeonatos já existentes.
        private static readonly Guid EsporteSeedId = new Guid("11111111-1111-1111-1111-111111111111");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Esportes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Esportes", x => x.Id);
                });

            // Seed do esporte padrão antes de adicionar as FKs obrigatórias.
            migrationBuilder.InsertData(
                table: "Esportes",
                columns: new[] { "Id", "Nome", "Icone", "Ativo", "CriadoEm" },
                values: new object[] { EsporteSeedId, "Futebol", "mdi:soccer", true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.AddColumn<Guid>(
                name: "EsporteId",
                table: "Times",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: EsporteSeedId);

            migrationBuilder.AddColumn<Guid>(
                name: "EsporteId",
                table: "Campeonatos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: EsporteSeedId);

            migrationBuilder.CreateIndex(
                name: "IX_Times_EsporteId",
                table: "Times",
                column: "EsporteId");

            migrationBuilder.CreateIndex(
                name: "IX_Campeonatos_EsporteId",
                table: "Campeonatos",
                column: "EsporteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campeonatos_Esportes_EsporteId",
                table: "Campeonatos",
                column: "EsporteId",
                principalTable: "Esportes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Times_Esportes_EsporteId",
                table: "Times",
                column: "EsporteId",
                principalTable: "Esportes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campeonatos_Esportes_EsporteId",
                table: "Campeonatos");

            migrationBuilder.DropForeignKey(
                name: "FK_Times_Esportes_EsporteId",
                table: "Times");

            migrationBuilder.DropTable(
                name: "Esportes");

            migrationBuilder.DropIndex(
                name: "IX_Times_EsporteId",
                table: "Times");

            migrationBuilder.DropIndex(
                name: "IX_Campeonatos_EsporteId",
                table: "Campeonatos");

            migrationBuilder.DropColumn(
                name: "EsporteId",
                table: "Times");

            migrationBuilder.DropColumn(
                name: "EsporteId",
                table: "Campeonatos");
        }
    }
}
