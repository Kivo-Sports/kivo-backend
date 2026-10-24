using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAutorExibicaoPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CampeonatoAutorId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TimeAutorId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoAutorExibicao",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CampeonatoAutorId",
                table: "Posts",
                column: "CampeonatoAutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TimeAutorId",
                table: "Posts",
                column: "TimeAutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Campeonatos_CampeonatoAutorId",
                table: "Posts",
                column: "CampeonatoAutorId",
                principalTable: "Campeonatos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Times_TimeAutorId",
                table: "Posts",
                column: "TimeAutorId",
                principalTable: "Times",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Campeonatos_CampeonatoAutorId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Times_TimeAutorId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CampeonatoAutorId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TimeAutorId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CampeonatoAutorId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TimeAutorId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TipoAutorExibicao",
                table: "Posts");
        }
    }
}
