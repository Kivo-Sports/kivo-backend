using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2DomainIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH ConvitesDuplicados AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY CampeonatoId, TimeId
                            ORDER BY
                                CASE EnumStatusParticipacao
                                    WHEN 1 THEN 0
                                    WHEN 0 THEN 1
                                    ELSE 2
                                END,
                                RespondidoEm DESC,
                                ConvidadoEm DESC,
                                Id
                        ) AS RowNumber
                    FROM CampeonatoTimes
                )
                DELETE FROM CampeonatoTimes
                WHERE Id IN (
                    SELECT Id
                    FROM ConvitesDuplicados
                    WHERE RowNumber > 1
                );
                """);

            migrationBuilder.Sql("""
                DELETE f
                FROM Favoritos f
                LEFT JOIN Times t ON t.Id = f.ItemId
                WHERE f.Tipo = 0 AND t.Id IS NULL;

                DELETE f
                FROM Favoritos f
                LEFT JOIN Campeonatos c ON c.Id = f.ItemId
                WHERE f.Tipo = 1 AND c.Id IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_CampeonatoTimes_CampeonatoId",
                table: "CampeonatoTimes");

            migrationBuilder.CreateIndex(
                name: "IX_CampeonatoTimes_CampeonatoId_TimeId",
                table: "CampeonatoTimes",
                columns: new[] { "CampeonatoId", "TimeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CampeonatoTimes_CampeonatoId_TimeId",
                table: "CampeonatoTimes");

            migrationBuilder.CreateIndex(
                name: "IX_CampeonatoTimes_CampeonatoId",
                table: "CampeonatoTimes",
                column: "CampeonatoId");
        }
    }
}
