using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoritos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_UsuarioId_Tipo_ItemId",
                table: "Favoritos",
                columns: new[] { "UsuarioId", "Tipo", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favoritos");
        }
    }
}
