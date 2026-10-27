using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTipoDeficiencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiposDeficiencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDeficiencia", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TiposDeficiencia",
                columns: new[] { "Id", "Descricao" },
                values: new object[,]
                {
                    { new Guid("d1a1b2c3-0001-4000-8000-000000000001"), "Nenhuma" },
                    { new Guid("d1a1b2c3-0002-4000-8000-000000000002"), "Pessoa com deficiência auditiva" },
                    { new Guid("d1a1b2c3-0003-4000-8000-000000000003"), "Cadeirante" }
                });

            migrationBuilder.AddColumn<Guid>(
                name: "TipoDeficienciaId",
                table: "Ingressos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("d1a1b2c3-0001-4000-8000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_Ingressos_TipoDeficienciaId",
                table: "Ingressos",
                column: "TipoDeficienciaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingressos_TiposDeficiencia_TipoDeficienciaId",
                table: "Ingressos",
                column: "TipoDeficienciaId",
                principalTable: "TiposDeficiencia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ingressos_TiposDeficiencia_TipoDeficienciaId",
                table: "Ingressos");

            migrationBuilder.DropTable(
                name: "TiposDeficiencia");

            migrationBuilder.DropIndex(
                name: "IX_Ingressos_TipoDeficienciaId",
                table: "Ingressos");

            migrationBuilder.DropColumn(
                name: "TipoDeficienciaId",
                table: "Ingressos");
        }
    }
}
