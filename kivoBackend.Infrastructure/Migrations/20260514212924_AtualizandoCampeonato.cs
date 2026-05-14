using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AtualizandoCampeonato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormatoCampeonato",
                table: "Campeonatos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Partida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampeonatoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeCasaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeVisitanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GolsTimeCasa = table.Column<int>(type: "int", nullable: false),
                    GolsTimeVisitante = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Local = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Finalizado = table.Column<bool>(type: "bit", nullable: false),
                    Rodada = table.Column<int>(type: "int", nullable: true),
                    Fase = table.Column<int>(type: "int", nullable: false),
                    NumeroJogoChave = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partida_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "Campeonatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Partida_Times_TimeCasaId",
                        column: x => x.TimeCasaId,
                        principalTable: "Times",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Partida_Times_TimeVisitanteId",
                        column: x => x.TimeVisitanteId,
                        principalTable: "Times",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partida_CampeonatoId",
                table: "Partida",
                column: "CampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_Partida_TimeCasaId",
                table: "Partida",
                column: "TimeCasaId");

            migrationBuilder.CreateIndex(
                name: "IX_Partida_TimeVisitanteId",
                table: "Partida",
                column: "TimeVisitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Partida");

            migrationBuilder.DropColumn(
                name: "FormatoCampeonato",
                table: "Campeonatos");
        }
    }
}
