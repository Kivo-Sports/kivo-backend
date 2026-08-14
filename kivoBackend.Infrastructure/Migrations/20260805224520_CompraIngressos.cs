using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompraIngressos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingressos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngressoLoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrecoPago = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DataCompra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusIngresso = table.Column<int>(type: "int", nullable: false),
                    CodigoValidacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataUso = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingressos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingressos_IngressoLotes_IngressoLoteId",
                        column: x => x.IngressoLoteId,
                        principalTable: "IngressoLotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ingressos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ingressos_IngressoLoteId",
                table: "Ingressos",
                column: "IngressoLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingressos_UsuarioId",
                table: "Ingressos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ingressos");
        }
    }
}
