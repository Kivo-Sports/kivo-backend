using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kivoBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovendoColunaSenhaTbUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Senha",
                table: "Usuarios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Senha",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
