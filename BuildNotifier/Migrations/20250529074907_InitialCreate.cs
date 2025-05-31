using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildNotifier.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanChats",
                columns: table => new
                {
                    PlanName = table.Column<string>(type: "TEXT", nullable: false),
                    ChatId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanChats", x => new { x.PlanName, x.ChatId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanChats_ChatId",
                table: "PlanChats",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanChats_PlanName",
                table: "PlanChats",
                column: "PlanName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanChats");
        }
    }
}
