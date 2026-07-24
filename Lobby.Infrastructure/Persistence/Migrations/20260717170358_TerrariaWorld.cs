using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lobby.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TerrariaWorld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PlayerCount",
                table: "server_instances",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingSince",
                table: "server_instances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorldId",
                table: "server_instances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "terraria_world_storages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AbsolutePathOnTheDisk = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terraria_world_storages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "terraria_worlds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StorageId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terraria_worlds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_terraria_worlds_players_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_terraria_worlds_terraria_world_storages_StorageId",
                        column: x => x.StorageId,
                        principalTable: "terraria_world_storages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_instances_WorldId",
                table: "server_instances",
                column: "WorldId");

            migrationBuilder.CreateIndex(
                name: "IX_terraria_worlds_OwnerId",
                table: "terraria_worlds",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_terraria_worlds_StorageId",
                table: "terraria_worlds",
                column: "StorageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_server_instances_terraria_worlds_WorldId",
                table: "server_instances",
                column: "WorldId",
                principalTable: "terraria_worlds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_server_instances_terraria_worlds_WorldId",
                table: "server_instances");

            migrationBuilder.DropTable(
                name: "terraria_worlds");

            migrationBuilder.DropTable(
                name: "terraria_world_storages");

            migrationBuilder.DropIndex(
                name: "IX_server_instances_WorldId",
                table: "server_instances");

            migrationBuilder.DropColumn(
                name: "PendingSince",
                table: "server_instances");

            migrationBuilder.DropColumn(
                name: "WorldId",
                table: "server_instances");

            migrationBuilder.AlterColumn<int>(
                name: "PlayerCount",
                table: "server_instances",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }
    }
}
