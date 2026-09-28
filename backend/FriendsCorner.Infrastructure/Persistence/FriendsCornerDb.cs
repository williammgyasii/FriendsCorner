using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Persistence;

public sealed class FriendsCornerDb(DbContextOptions<FriendsCornerDb> options) : DbContext(options)
{
    public DbSet<TicTacToeRow> TicTacToe => Set<TicTacToeRow>();
    public DbSet<ChessRow> Chess => Set<ChessRow>();

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
    }
}
