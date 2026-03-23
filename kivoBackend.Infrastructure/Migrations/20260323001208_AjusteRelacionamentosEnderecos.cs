using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjusteRelacionamentosEnderecos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizadoresCampeonato_Enderecos_EnderecoId",
                table: "OrganizadoresCampeonato");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizadoresTime_Enderecos_EnderecoId",
                table: "OrganizadoresTime");

            migrationBuilder.DropForeignKey(
                name: "FK_Torcedores_Enderecos_EnderecoId",
                table: "Torcedores");

            migrationBuilder.DropIndex(
                name: "IX_Torcedores_EnderecoId",
                table: "Torcedores");

            migrationBuilder.DropIndex(
                name: "IX_OrganizadoresTime_EnderecoId",
                table: "OrganizadoresTime");

            migrationBuilder.DropIndex(
                name: "IX_OrganizadoresCampeonato_EnderecoId",
                table: "OrganizadoresCampeonato");

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

            migrationBuilder.CreateIndex(
                name: "IX_Torcedores_EnderecoId",
                table: "Torcedores",
                column: "EnderecoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresTime_EnderecoId",
                table: "OrganizadoresTime",
                column: "EnderecoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresCampeonato_EnderecoId",
                table: "OrganizadoresCampeonato",
                column: "EnderecoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizadoresCampeonato_Enderecos_EnderecoId",
                table: "OrganizadoresCampeonato",
                column: "EnderecoId",
                principalTable: "Enderecos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizadoresTime_Enderecos_EnderecoId",
                table: "OrganizadoresTime",
                column: "EnderecoId",
                principalTable: "Enderecos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Torcedores_Enderecos_EnderecoId",
                table: "Torcedores",
                column: "EnderecoId",
                principalTable: "Enderecos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizadoresCampeonato_Enderecos_EnderecoId",
                table: "OrganizadoresCampeonato");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizadoresTime_Enderecos_EnderecoId",
                table: "OrganizadoresTime");

            migrationBuilder.DropForeignKey(
                name: "FK_Torcedores_Enderecos_EnderecoId",
                table: "Torcedores");

            migrationBuilder.DropIndex(
                name: "IX_Torcedores_EnderecoId",
                table: "Torcedores");

            migrationBuilder.DropIndex(
                name: "IX_OrganizadoresTime_EnderecoId",
                table: "OrganizadoresTime");

            migrationBuilder.DropIndex(
                name: "IX_OrganizadoresCampeonato_EnderecoId",
                table: "OrganizadoresCampeonato");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CampeonatoTimes",
                table: "CampeonatoTimes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CampeonatoTimes",
                table: "CampeonatoTimes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Torcedores_EnderecoId",
                table: "Torcedores",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresTime_EnderecoId",
                table: "OrganizadoresTime",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizadoresCampeonato_EnderecoId",
                table: "OrganizadoresCampeonato",
                column: "EnderecoId");

            migrationBuilder.CreateIndex(
                name: "IX_CampeonatoTimes_CampeonatoId",
                table: "CampeonatoTimes",
                column: "CampeonatoId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizadoresCampeonato_Enderecos_EnderecoId",
                table: "OrganizadoresCampeonato",
                column: "EnderecoId",
                principalTable: "Enderecos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizadoresTime_Enderecos_EnderecoId",
                table: "OrganizadoresTime",
                column: "EnderecoId",
                principalTable: "Enderecos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Torcedores_Enderecos_EnderecoId",
                table: "Torcedores",
                column: "EnderecoId",
                principalTable: "Enderecos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
