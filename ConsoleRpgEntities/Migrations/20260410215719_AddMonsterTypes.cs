using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsoleRpgEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasArmor",
                table: "Monsters",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackSize",
                table: "Monsters",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasArmor",
                table: "Monsters");

            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "Monsters");
        }
    }
}
