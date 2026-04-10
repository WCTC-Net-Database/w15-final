using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsoleRpgEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddChestLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocationRoomId",
                table: "Containers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Containers_LocationRoomId",
                table: "Containers",
                column: "LocationRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Containers_Containers_LocationRoomId",
                table: "Containers",
                column: "LocationRoomId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Containers_Containers_LocationRoomId",
                table: "Containers");

            migrationBuilder.DropIndex(
                name: "IX_Containers_LocationRoomId",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "LocationRoomId",
                table: "Containers");
        }
    }
}
