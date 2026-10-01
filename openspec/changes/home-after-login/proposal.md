## Why

A signed-in visit to `/` with no room opens a room immediately from an "Open a lobby" screen. The person needs a home first, and the room with an id to share comes only after they start a session.

## What Changes

- A signed-in visit to `/` with no room shows a home: the account's game name, the plan name when they have one, and a **Start a session** action. It does not show "Open a lobby".
- **Start a session** creates a room the same way the current button does, then opens `/?room=<id>`.
- A signed-in visit that already has `?room=<id>` still enters that table and skips the home.
- A signed-in visit with `?plan=` and no current plan still starts checkout before the home.
- Billing pages, settings, friends, groups, and metrics stay later. The marketing site does not change.

## Capabilities

### New Capabilities

- `account-home`: the signed-in home that shows the game name and plan, and starts a session only when asked.

### Modified Capabilities

## Impact

- The play app's signed-in screen at `/` changes. `GET /account` already returns the game name and the plan, so no new API or table is required.
- `POST /rooms` stays the call that creates the room.
- Checkout for `?plan=` stays in front of the home.
