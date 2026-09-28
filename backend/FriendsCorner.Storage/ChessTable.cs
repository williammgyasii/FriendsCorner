using FriendsCorner;
using Npgsql;

namespace FriendsCorner.Storage;

public sealed class ChessTable
{
    private readonly string _connectionString;

    public ChessTable(string connectionString) =>
        _connectionString = NeonConnection.ToNpgsql(connectionString);

    public async Task Save(string roomId, ChessBoard board)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            """
            insert into chess (room_id, fen, white_seat)
            values (@room_id, @fen, @white_seat)
            on conflict (room_id) do update
            set fen = excluded.fen,
                white_seat = excluded.white_seat
            """,
            connection);
        command.Parameters.AddWithValue("room_id", roomId);
        command.Parameters.AddWithValue("fen", board.Fen);
        command.Parameters.AddWithValue("white_seat", board.White.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<ChessBoard?> Load(string roomId)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            "select fen, white_seat from chess where room_id = @room_id",
            connection);
        command.Parameters.AddWithValue("room_id", roomId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var white = reader.GetString(1) == nameof(Seat.B) ? Seat.B : Seat.A;
        return ChessBoard.TryFromFen(reader.GetString(0), white, out var board) ? board : null;
    }

    public async Task Remove(string roomId)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            "delete from chess where room_id = @room_id",
            connection);
        command.Parameters.AddWithValue("room_id", roomId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<NpgsqlConnection> Open()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var ensure = new NpgsqlCommand(
            """
            create table if not exists chess (
                room_id text primary key,
                fen text not null,
                white_seat text not null
            )
            """,
            connection);
        await ensure.ExecuteNonQueryAsync();
        return connection;
    }
}
