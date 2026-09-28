# Spec Delta

## Purpose

Let the two people in a room see and hear each other without sending the camera through the movement rules.

## ADDED Requirements

### Requirement: The other person's face appears after both have joined

When two seats are in the room and both browsers allow the camera, each page MUST show the other person's live camera next to the floor. The page MUST NOT show a face tile for an empty seat.

#### Scenario: The second person joins

- **WHEN** seat B joins a room that already has seat A, and both allow the camera
- **THEN** each page shows one live picture of the other person

#### Scenario: The other person leaves

- **WHEN** the other seat's socket closes
- **THEN** that person's picture is removed and the remaining person can still move

### Requirement: Microphones are heard

When both browsers allow the microphone, each person MUST hear the other. A person MUST NOT hear their own microphone played back.

#### Scenario: Both microphones are allowed

- **WHEN** both seats have joined and both browsers allow the microphone
- **THEN** each person hears the other and does not hear their own voice played back by the page

### Requirement: Refusing the camera does not block movement

If a browser denies the camera or the microphone, that person MUST still send directions and see positions. The other page MUST show that the face is unavailable instead of a live picture.

#### Scenario: The camera is denied

- **WHEN** seat B denies the camera and then holds a direction
- **THEN** seat B's character still moves, and seat A's page shows that B's face is unavailable

### Requirement: The server does not carry the picture

Camera and microphone bytes MUST travel between the two browsers. The server MUST only pass introduction messages from one seat to the other. A direction message MUST still change only that seat's movement.

#### Scenario: An introduction message arrives

- **WHEN** seat A sends an introduction message
- **THEN** seat B receives that message and seat A's stored direction is unchanged

### Requirement: A third person gets no face

A third connection MUST still be refused. It MUST NOT receive a camera or a microphone from either seat.

#### Scenario: A third browser opens the link

- **WHEN** seats A and B are already connected and a third socket opens the same room
- **THEN** the third socket is closed and neither existing face tile is replaced
