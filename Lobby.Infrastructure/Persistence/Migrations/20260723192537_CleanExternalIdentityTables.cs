using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lobby.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CleanExternalIdentityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_google_logins");

            migrationBuilder.CreateTable(
                name: "player_external_identities",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    external_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_external_identities", x => new { x.player_id, x.provider });
                    table.ForeignKey(
                        name: "FK_player_external_identities_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_external_identities_provider_external_id",
                table: "player_external_identities",
                columns: new[] { "provider", "external_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_external_identities");

            migrationBuilder.CreateTable(
                name: "player_google_logins",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    google_login = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_google_logins", x => x.player_id);
                    table.ForeignKey(
                        name: "FK_player_google_logins_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_google_logins_google_login",
                table: "player_google_logins",
                column: "google_login",
                unique: true);
        }
    }
}
