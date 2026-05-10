using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spx.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VisibleThroughMessageId",
                table: "GamePlayers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SenderPlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientPlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Body = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SenderDisplayName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RecipientDisplayName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameMessages_GamePlayers_RecipientPlayerId",
                        column: x => x.RecipientPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameMessages_GamePlayers_SenderPlayerId",
                        column: x => x.SenderPlayerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameMessages_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameMessages_GameId_Id",
                table: "GameMessages",
                columns: new[] { "GameId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GameMessages_RecipientPlayerId",
                table: "GameMessages",
                column: "RecipientPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMessages_SenderPlayerId",
                table: "GameMessages",
                column: "SenderPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameMessages");

            migrationBuilder.DropColumn(
                name: "VisibleThroughMessageId",
                table: "GamePlayers");
        }
    }
}
