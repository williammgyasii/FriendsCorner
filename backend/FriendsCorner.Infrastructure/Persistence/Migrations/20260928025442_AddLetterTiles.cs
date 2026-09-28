using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendsCorner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterTiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "letter_tiles",
                columns: table => new
                {
                    room_id = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_letter_tiles", x => x.room_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "letter_tiles");
        }
    }
}
