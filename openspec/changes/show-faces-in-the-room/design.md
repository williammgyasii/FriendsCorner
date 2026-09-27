# Design

## Context

See proposal.md for why. Today `Room` owns positions. `RoomSession` owns the two sockets and calls `Tick`. The Vite page draws circles from the state message. There is no spec inventory yet.

## Goals / Non-Goals

**Goals:**

- The page draws a wall, a floor, and two figures from the positions it already receives.
- `RoomSession` forwards an introduction message from one socket to the other.
- The page opens one peer connection for camera and mic after that introduction.

**Non-Goals:**

- Teaching `Room` about cameras, colors, or pixels.
- A media server, recording, or a third video participant.
- Score, collectibles, or persistence.

## Decisions

1. **The look is a page function.** A pure function takes the two positions and returns where each figure is drawn, including the 24-pixel separation and the inset that keeps a figure inside the floor. The canvas calls that function. `Room` still reports `(400, 160)` when that is the true position.

2. **Introduction messages ride the existing socket.** A message whose type is `signal` is copied to the other seat. `Room.TrySetDirection` is not called. A separate media socket was the alternative. One socket keeps "who you are" as the seat the server already assigned.

3. **The browsers own the media.** The page creates the peer connection, the camera, and the mic. `RoomSession` does not parse the introduction body and does not reference WebRTC. The alternative was the server forwarding frames. That would put video in the session and make the server the media path.

4. **The face tile is beside the floor, not inside `Room`.** Denying the camera leaves the movement path untouched. The tile shows an unavailable state.

## Risks / Trade-offs

- [A browser blocks the camera] → Movement still works. The tile says the face is unavailable.
- [Introduction messages and direction messages share a socket] → The session branches on message type before it touches `Room`. A test locks that branch.
- [Two figures overlap at the start] → The page separates the drawing. The engine positions stay equal.
- [WebRTC fails across networks later] → This slice is the two local browsers. A relay server is a later change, not this one.
