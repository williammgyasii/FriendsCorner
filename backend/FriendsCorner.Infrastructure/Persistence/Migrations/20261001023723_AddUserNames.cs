using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendsCorner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "game_name",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET name = split_part(email, '@', 1),
                    game_name = split_part(email, '@', 1)
                WHERE name IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "game_name",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "game_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "name",
                table: "users");
        }
    }
}
