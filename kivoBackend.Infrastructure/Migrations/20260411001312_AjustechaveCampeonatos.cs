using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjustechaveCampeonatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CampeonatoTimes",
                table: "CampeonatoTimes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CampeonatoTimes",
                table: "CampeonatoTimes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_CampeonatoTimes_CampeonatoId",
                table: "CampeonatoTimes",
                column: "CampeonatoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CampeonatoTimes",
                table: "CampeonatoTimes");

            migrationBuilder.DropIndex(
                name: "IX_CampeonatoTimes_CampeonatoId",
                table: "CampeonatoTimes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CampeonatoTimes",
                table: "CampeonatoTimes",
                columns: new[] { "CampeonatoId", "TimeId" });
        }
    }
}
