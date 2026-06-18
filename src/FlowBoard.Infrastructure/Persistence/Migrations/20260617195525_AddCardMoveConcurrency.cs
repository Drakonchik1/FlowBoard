using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardMoveConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cards_BoardListId_Position",
                table: "cards");

            migrationBuilder.CreateIndex(
                name: "IX_cards_BoardListId_Position",
                table: "cards",
                columns: new[] { "BoardListId", "Position" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cards_BoardListId_Position",
                table: "cards");

            migrationBuilder.CreateIndex(
                name: "IX_cards_BoardListId_Position",
                table: "cards",
                columns: new[] { "BoardListId", "Position" });
        }
    }
}
