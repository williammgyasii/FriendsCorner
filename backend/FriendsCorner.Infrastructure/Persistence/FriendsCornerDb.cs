using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Persistence;

public sealed class FriendsCornerDb(DbContextOptions<FriendsCornerDb> options) : DbContext(options)
{
    public DbSet<TicTacToeRow> TicTacToe => Set<TicTacToeRow>();
    public DbSet<ChessRow> Chess => Set<ChessRow>();
    public DbSet<LetterTilesRow> LetterTiles => Set<LetterTilesRow>();
    public DbSet<MysteryRow> Mystery => Set<MysteryRow>();
    public DbSet<UserRow> Users => Set<UserRow>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TicTacToeRow>(row =>
        {
            row.ToTable("tic_tac_toe");
            row.HasKey(r => r.RoomId);
            row.Property(r => r.RoomId).HasColumnName("room_id");
            row.Property(r => r.Squares).HasColumnName("squares").HasColumnType("char(9)");
            row.Property(r => r.NextSeat).HasColumnName("next_seat");
        });

        model.Entity<ChessRow>(row =>
        {
            row.ToTable("chess");
            row.HasKey(r => r.RoomId);
            row.Property(r => r.RoomId).HasColumnName("room_id");
            row.Property(r => r.Fen).HasColumnName("fen");
            row.Property(r => r.WhiteSeat).HasColumnName("white_seat");
        });

        model.Entity<LetterTilesRow>(row =>
        {
            row.ToTable("letter_tiles");
            row.HasKey(r => r.RoomId);
            row.Property(r => r.RoomId).HasColumnName("room_id");
            row.Property(r => r.State).HasColumnName("state").HasColumnType("jsonb");
        });

        model.Entity<MysteryRow>(row =>
        {
            row.ToTable("mystery_games");
            row.HasKey(r => r.RoomId);
            row.Property(r => r.RoomId).HasColumnName("room_id");
            row.Property(r => r.State).HasColumnName("state").HasColumnType("jsonb");
        });

        model.Entity<UserRow>(row =>
        {
            row.ToTable("users");
            row.HasKey(r => r.Id);
            row.Property(r => r.Id).HasColumnName("id");
            row.Property(r => r.Email).HasColumnName("email");
            row.Property(r => r.PasswordHash).HasColumnName("password_hash");
            row.Property(r => r.Name).HasColumnName("name");
            row.Property(r => r.GameName).HasColumnName("game_name");
            row.Property(r => r.StripeCustomerId).HasColumnName("stripe_customer_id");
            row.Property(r => r.Plan).HasColumnName("plan");
            row.Property(r => r.BillingEventAt).HasColumnName("billing_event_at");
            row.HasIndex(r => r.Email).IsUnique();
            row.HasIndex(r => r.StripeCustomerId).IsUnique();
        });
    }
}
