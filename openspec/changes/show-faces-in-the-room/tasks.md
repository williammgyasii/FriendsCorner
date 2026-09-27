# Tasks

## 1. Character layout

- [x] 1.1 Write a failing test that two figures at `(240, 160)` have drawn centers at least 24 pixels apart, the wall band is 64 pixels, and a figure at `(480, 0)` stays inside the 480 by 320 floor. Run the test and confirm it fails.
- [x] 1.2 Implement the layout function so that test passes. Confirm the existing seven room tests still pass.

## 2. Draw the room

- [x] 2.1 Draw the wall, the floor, and one figure per occupied seat from the layout function. Open the page and confirm one seat shows one figure, and two seats on the same spot show two figures.

## 3. Introduction messages

- [x] 3.1 Write a failing test that a `signal` message is marked for the other seat and does not change the stored direction, while a `direction` message still sets the direction. Run it and confirm it fails.
- [x] 3.2 Teach `RoomSession` to copy a `signal` message to the other socket and to leave `Room.TrySetDirection` for direction messages only. Confirm the new test passes and the seven room tests still pass.

## 4. Faces

- [x] 4.1 Write a failing test that a denied camera sets the face tile to unavailable and leaves movement allowed. Run it and confirm it fails.
- [x] 4.2 Open the peer connection from the introduction messages and show the other person's camera and microphone, using the unavailable state when permission is denied. With two browsers, confirm each person sees and hears the other, a denial still moves the character, and a third connection is still closed because the room is full.
