using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendsCorner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHostPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "billing_event_at",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "plan",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_stripe_customer_id",
                table: "users",
                column: "stripe_customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_stripe_customer_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "billing_event_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "plan",
                table: "users");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "users");
        }
    }
}
