# Proposal

## Why

Two dots on a gray rectangle do not feel like being in a room together. The movement already works. The missing piece is seeing and hearing the other person, and a floor that looks like a place you are both standing in.

## What Changes

- Each seat is drawn as a small character on a room floor, not as a plain circle on a gray box. The two characters stay distinguishable when they stand on the same spot.
- After both people have joined, each browser shows the other person's camera and plays their microphone.
- The movement socket stays direction in, positions out. Video and audio use a second connection between the two browsers.
- The server introduces the browsers to each other. It does not forward video frames.
- If a person denies the camera or mic, they still move. The other person sees that the face is unavailable.
- A third person is still refused.

Stays later:

- A score, stars, or any goal inside the room.
- A room that survives a server restart.
- Watching a video together.
- Accounts.

Assumption: "gamify a bit" means the look of the room and the characters. It does not add rules, points, or a win state.

## Capabilities

### New Capabilities

- `room-look`: The floor is a small room, and each seat is a character the other person can tell apart.
- `faces`: The two browsers exchange camera and microphone after the server introduces them.

### Modified Capabilities

- None. There is no existing spec yet. Movement behavior stays as it is.

## Impact

- The Vite page gains the room drawing and a face tile. `Room` does not learn about cameras.
- `RoomSession` gains a way to pass introduction messages between the two sockets. It does not carry media.
- The browsers use WebRTC for the picture and sound. No new server media dependency.
