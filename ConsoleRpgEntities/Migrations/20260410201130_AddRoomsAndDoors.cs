using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsoleRpgEntities.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomsAndDoors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentRoomId",
                table: "Players",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoomId",
                table: "Monsters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EastRoomId",
                table: "Containers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Containers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NorthRoomId",
                table: "Containers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Room_Description",
                table: "Containers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SouthRoomId",
                table: "Containers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WestRoomId",
                table: "Containers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "X",
                table: "Containers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Y",
                table: "Containers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Doors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoomAId = table.Column<int>(type: "int", nullable: false),
                    RoomBId = table.Column<int>(type: "int", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    IsTrapped = table.Column<bool>(type: "bit", nullable: false),
                    RequiredKeyId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPickable = table.Column<bool>(type: "bit", nullable: false),
                    TrapDisarmed = table.Column<bool>(type: "bit", nullable: false),
                    TrapDamage = table.Column<int>(type: "int", nullable: false),
                    IsSecret = table.Column<bool>(type: "bit", nullable: false),
                    IsDiscovered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doors_Containers_RoomAId",
                        column: x => x.RoomAId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Doors_Containers_RoomBId",
                        column: x => x.RoomBId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_CurrentRoomId",
                table: "Players",
                column: "CurrentRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Monsters_CurrentRoomId",
                table: "Monsters",
                column: "CurrentRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_EastRoomId",
                table: "Containers",
                column: "EastRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_NorthRoomId",
                table: "Containers",
                column: "NorthRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_SouthRoomId",
                table: "Containers",
                column: "SouthRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_WestRoomId",
                table: "Containers",
                column: "WestRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Doors_RoomAId",
                table: "Doors",
                column: "RoomAId");

            migrationBuilder.CreateIndex(
                name: "IX_Doors_RoomBId",
                table: "Doors",
                column: "RoomBId");

            migrationBuilder.AddForeignKey(
                name: "FK_Containers_Containers_EastRoomId",
                table: "Containers",
                column: "EastRoomId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Containers_Containers_NorthRoomId",
                table: "Containers",
                column: "NorthRoomId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Containers_Containers_SouthRoomId",
                table: "Containers",
                column: "SouthRoomId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Containers_Containers_WestRoomId",
                table: "Containers",
                column: "WestRoomId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Monsters_Containers_CurrentRoomId",
                table: "Monsters",
                column: "CurrentRoomId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Containers_CurrentRoomId",
                table: "Players",
                column: "CurrentRoomId",
                principalTable: "Containers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Containers_Containers_EastRoomId",
                table: "Containers");

            migrationBuilder.DropForeignKey(
                name: "FK_Containers_Containers_NorthRoomId",
                table: "Containers");

            migrationBuilder.DropForeignKey(
                name: "FK_Containers_Containers_SouthRoomId",
                table: "Containers");

            migrationBuilder.DropForeignKey(
                name: "FK_Containers_Containers_WestRoomId",
                table: "Containers");

            migrationBuilder.DropForeignKey(
                name: "FK_Monsters_Containers_CurrentRoomId",
                table: "Monsters");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Containers_CurrentRoomId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "Doors");

            migrationBuilder.DropIndex(
                name: "IX_Players_CurrentRoomId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Monsters_CurrentRoomId",
                table: "Monsters");

            migrationBuilder.DropIndex(
                name: "IX_Containers_EastRoomId",
                table: "Containers");

            migrationBuilder.DropIndex(
                name: "IX_Containers_NorthRoomId",
                table: "Containers");

            migrationBuilder.DropIndex(
                name: "IX_Containers_SouthRoomId",
                table: "Containers");

            migrationBuilder.DropIndex(
                name: "IX_Containers_WestRoomId",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "CurrentRoomId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CurrentRoomId",
                table: "Monsters");

            migrationBuilder.DropColumn(
                name: "EastRoomId",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "NorthRoomId",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "Room_Description",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "SouthRoomId",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "WestRoomId",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "X",
                table: "Containers");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "Containers");
        }
    }
}
