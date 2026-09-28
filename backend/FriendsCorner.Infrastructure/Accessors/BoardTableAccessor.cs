using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Accessors;

public sealed class BoardTableAccessor(IDbContextFactory<FriendsCornerDb> contexts) : IBoardTableAccessor
{
    // Find-then-write is safe because each room's recorder is its only writer.
    public async Task Save(string roomId, Board board)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.TicTacToe.FindAsync(roomId);
        if (row is null)
        {
            db.TicTacToe.Add(new TicTacToeRow
            {
                RoomId = roomId,
                Squares = board.SquaresRow(),
                NextSeat = board.Next.ToString(),
            });
        }
        else
        {
            row.Squares = board.SquaresRow();
            row.NextSeat = board.Next.ToString();
        }

        await db.SaveChangesAsync();
    }

    public async Task<Board?> Load(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.TicTacToe.AsNoTracking().SingleOrDefaultAsync(r => r.RoomId == roomId);
        return row is null ? null : Board.FromRow(row.Squares, row.NextSeat);
    }

    public async Task Remove(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        await db.TicTacToe.Where(r => r.RoomId == roomId).ExecuteDeleteAsync();
    }
}
