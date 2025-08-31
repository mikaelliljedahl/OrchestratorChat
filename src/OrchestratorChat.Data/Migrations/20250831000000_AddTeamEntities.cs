using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrchestratorChat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    PoliciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => new { x.TeamId, x.AgentId });
                    table.ForeignKey(
                        name: "FK_TeamMembers_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedAt",
                table: "Teams",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SessionId",
                table: "Teams",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_AgentId",
                table: "TeamMembers",
                column: "AgentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}