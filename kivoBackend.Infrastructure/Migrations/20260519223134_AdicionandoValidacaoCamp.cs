using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionandoValidacaoCamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partida_Times_TimeCasaId",
                table: "Partida");

            migrationBuilder.DropForeignKey(
                name: "FK_Partida_Times_TimeVisitanteId",
                table: "Partida");

            migrationBuilder.AlterColumn<int>(
                name: "PontosVitoria",
                table: "Campeonatos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PontosEmpate",
                table: "Campeonatos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PontosDerrota",
                table: "Campeonatos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Partida_Times_TimeCasaId",
                table: "Partida",
                column: "TimeCasaId",
                principalTable: "Times",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Partida_Times_TimeVisitanteId",
                table: "Partida",
                column: "TimeVisitanteId",
                principalTable: "Times",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partida_Times_TimeCasaId",
                table: "Partida");

            migrationBuilder.DropForeignKey(
                name: "FK_Partida_Times_TimeVisitanteId",
                table: "Partida");

            migrationBuilder.AlterColumn<int>(
                name: "PontosVitoria",
                table: "Campeonatos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PontosEmpate",
                table: "Campeonatos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PontosDerrota",
                table: "Campeonatos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Partida_Times_TimeCasaId",
                table: "Partida",
                column: "TimeCasaId",
                principalTable: "Times",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Partida_Times_TimeVisitanteId",
                table: "Partida",
                column: "TimeVisitanteId",
                principalTable: "Times",
                principalColumn: "Id");
        }
    }
}
