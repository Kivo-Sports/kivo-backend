using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionandoCampeaoCampeonato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TimeVencedorId",
                table: "Campeonatos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campeonatos_TimeVencedorId",
                table: "Campeonatos",
                column: "TimeVencedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campeonatos_Times_TimeVencedorId",
                table: "Campeonatos",
                column: "TimeVencedorId",
                principalTable: "Times",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campeonatos_Times_TimeVencedorId",
                table: "Campeonatos");

            migrationBuilder.DropIndex(
                name: "IX_Campeonatos_TimeVencedorId",
                table: "Campeonatos");

            migrationBuilder.DropColumn(
                name: "TimeVencedorId",
                table: "Campeonatos");
        }
    }
}
