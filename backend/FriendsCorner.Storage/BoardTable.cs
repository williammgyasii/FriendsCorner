using FriendsCorner;
using Npgsql;

namespace FriendsCorner.Storage;

public sealed class BoardTable
{
    private readonly string _connectionString;

    public BoardTable(string connectionString) =>
        _connectionString = NeonConnection.ToNpgsql(connectionString);

    public async Task Save(string roomId, Board board)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            """
            insert into tic_tac_toe (room_id, squares, next_seat)
            values (@room_id, @squares, @next_seat)
            on conflict (room_id) do update
            set squares = excluded.squares,
                next_seat = excluded.next_seat
            """,
            connection);
        command.Parameters.AddWithValue("room_id", roomId);
        command.Parameters.AddWithValue("squares", board.SquaresRow());
        command.Parameters.AddWithValue("next_seat", board.Next.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Board?> Load(string roomId)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            "select squares, next_seat from tic_tac_toe where room_id = @room_id",
            connection);
        command.Parameters.AddWithValue("room_id", roomId);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return Board.FromRow(reader.GetString(0), reader.GetString(1));
    }

    public async Task Remove(string roomId)
    {
        await using var connection = await Open();
        await using var command = new NpgsqlCommand(
            "delete from tic_tac_toe where room_id = @room_id",
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
            create table if not exists tic_tac_toe (
                room_id text primary key,
                squares char(9) not null,
                next_seat text not null
            )
            """,
            connection);
        await ensure.ExecuteNonQueryAsync();
        return connection;
    }
}
