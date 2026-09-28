using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendsCorner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chess",
                columns: table => new
                {
                    room_id = table.Column<string>(type: "text", nullable: false),
                    fen = table.Column<string>(type: "text", nullable: false),
                    white_seat = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chess", x => x.room_id);
                });

            migrationBuilder.CreateTable(
                name: "tic_tac_toe",
                columns: table => new
                {
                    room_id = table.Column<string>(type: "text", nullable: false),
                    squares = table.Column<string>(type: "char(9)", nullable: false),
                    next_seat = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tic_tac_toe", x => x.room_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chess");

            migrationBuilder.DropTable(
                name: "tic_tac_toe");
        }
    }
}
