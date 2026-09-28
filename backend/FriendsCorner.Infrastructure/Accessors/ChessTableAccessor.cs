using FriendsCorner.Core.Accessors;
using FriendsCorner.Core.Engines;
using FriendsCorner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FriendsCorner.Infrastructure.Accessors;

public sealed class ChessTableAccessor(IDbContextFactory<FriendsCornerDb> contexts) : IChessTableAccessor
{
    // Find-then-write is safe because each room's recorder is its only writer.
    public async Task Save(string roomId, ChessBoard board)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Chess.FindAsync(roomId);
        if (row is null)
        {
            db.Chess.Add(new ChessRow
            {
                RoomId = roomId,
                Fen = board.Fen,
                WhiteSeat = board.White.ToString(),
            });
        }
        else
        {
            row.Fen = board.Fen;
            row.WhiteSeat = board.White.ToString();
        }

        await db.SaveChangesAsync();
    }

    public async Task<ChessBoard?> Load(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        var row = await db.Chess.AsNoTracking().SingleOrDefaultAsync(r => r.RoomId == roomId);
        if (row is null)
        {
            return null;
        }

        var white = row.WhiteSeat == nameof(Seat.B) ? Seat.B : Seat.A;
        return ChessBoard.TryFromFen(row.Fen, white, out var board) ? board : null;
    }

    public async Task Remove(string roomId)
    {
        await using var db = await contexts.CreateDbContextAsync();
        await db.Chess.Where(r => r.RoomId == roomId).ExecuteDeleteAsync();
    }
}
