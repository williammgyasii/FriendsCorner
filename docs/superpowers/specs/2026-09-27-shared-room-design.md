# Shared room — first slice

Date: 2026-09-27

Two people open one link and see each other move. This slice is that path only.

## Product boundary

In this version:

- One person starts a room and receives a link. The other person opens it. A third person is told the room is full.
- Each person is a character. A key or a thumb sends one direction.
- The server stores both positions, applies the direction, and sends both positions back. Both screens draw those positions.
- Laptop and phone both work. Both controls emit the same direction message.

Later, in order:

1. Camera and mic, on their own connection.
2. A gentle goal in the room.
3. A room that is still there after a restart.
4. Watching something together.

## Stack

One git repository. Two processes in development.

| Piece | Role |
|---|---|
| `backend/FriendsCorner.Room` | Rules. No sockets. The type is `FriendsCorner.Room`. |
| `backend/FriendsCorner.Server` | ASP.NET Core on .NET 10. WebSocket endpoint and the 20 Hz clock. |
| `backend/FriendsCorner.Room.Tests` | xUnit, namespace `FriendsCorner.Tests`. References the room library, not the server. |
| `frontend` | Vite and TypeScript. Canvas, keys, thumb, browser `WebSocket`. |

The browser and the server do not share a type system. The direction message is a C# record and a TypeScript type, kept in sync by hand.

Vite serves the page. Kestrel serves the socket. Vite proxies the socket so the page uses one origin.

The socket is ASP.NET Core's WebSocket support. SignalR is a later step.

## Movement

- Seats are `A` and `B`. A new seat starts at `(240, 160)` with direction `(0, 0)`.
- Speed is `160` pixels per second. The floor is `480` by `320`.
- The client sends `{ "type": "direction", "x": ..., "y": ... }` when the direction changes. Each component is from `-1` to `1`. Releasing the control sends `(0, 0)`.
- The message does not contain a seat. The socket the server accepted is the seat.
- The server stores the direction. Twenty times a second it calls `Tick(0.05)`.
- `Tick` caps the direction length at `1`, then adds `direction × 160 × seconds`, then clamps to the floor.
- The server sends both positions to both sockets. Each page draws its own seat from the seat assigned at connect.

## Failure

- A socket that closes frees that seat, sets nothing moving for it, and sends positions immediately. The last close stops the clock and deletes the room.
- A new socket takes an open seat. A reload does not keep the previous seat.
- A message that is not a direction, or a component outside `-1` to `1`, does not change the stored direction and does not close the socket.
- `Start` creates the room id. An unknown id closes the socket. The page says the room is gone. A third person closes. The page says the room is full. The people already inside stay.

## First tests

These call the room only. `Tick` receives elapsed seconds. The tests do not sleep and do not open a socket.

1. Right for one second. Seats A and B exist. A's direction is `(1, 0)`. After one second, A is `(400, 160)`. B is `(240, 160)`.
2. A diagonal is not faster. A's direction is `(1, 1)`. After one second, A is `160` pixels from the center.
3. The floor holds you. A's direction is `(1, 0)`. After two seconds, A's x is `480`.
4. Standing still. A's direction is `(0, 0)`. After one second, A is `(240, 160)`.
5. A bad direction is refused. A is `(1, 0)`. Setting `(2, 0)` leaves `(1, 0)` in place. After one second, A is `(400, 160)`.
6. A freed seat is gone. A is moving right. The seat is freed. After one second, A is not in the positions.
7. Two seats. The third seat is refused. The first two stay.

Session tests (unknown id, leave, full room) come after these seven pass. They need sockets.
